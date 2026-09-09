using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Lyo.Reporting.Tests.Postgres;

/// <summary>
/// Integration coverage for reporting hardening: stuck-run recovery, retention resilience to failing cleanup hooks, generation wall-clock timeout, provider failure audit
/// trail, OutputFileId persistence on failure, and the unique definition-parameter key index.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReportResilienceTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    private IDbContextFactory<ReportingContext> DbFactory => fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();

    private static string SampleReportJson()
        => ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("Resilience").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddRow("1"))).Build());

    private ReportRetentionService CreateRetentionService(PostgresReportingOptions options, ReportGenerationLiveTracker? liveTracker = null)
        => new(DbFactory, fixture.ServiceProvider, options, fixture.ServiceProvider.GetRequiredService<ILogger<ReportRetentionService>>(), liveTracker: liveTracker);

    private ReportService CreateReportService(IServiceProvider sp, PostgresReportingOptions options, IReadOnlyList<IReportDataProvider>? providers = null)
    {
        // These checks generate from ad-hoc composition JSON, which the shipped default refuses.
        options.AllowAdHocGeneration = true;
        return new(
            sp.GetRequiredService<IDbContextFactory<ReportingContext>>(), sp.GetServices<IReportRenderer>(), providers ?? [], [], sp, options,
            sp.GetRequiredService<ILogger<ReportService>>());
    }

    [Fact]
    public async Task Stuck_RecoveryMarksStalePendingAndRunning_Failed()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var stalePending = NewGeneration(nameof(ReportGenerationStatus.Pending), DateTime.UtcNow.AddHours(-3));
        var staleRunning = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-3));
        var freshRunning = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow, DateTime.UtcNow);
        db.ReportGenerations.AddRange(stalePending, staleRunning, freshRunning);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateRetentionService(new() { ConnectionString = "unused", StuckGenerationTimeout = TimeSpan.FromHours(1) });
        var recovered = await service.RecoverStuckGenerationsAsync(TestContext.Current.CancellationToken);
        Assert.True(recovered >= 2, $"Expected at least the two stale rows to be recovered, got {recovered}.");
        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var pending = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == stalePending.Id, TestContext.Current.CancellationToken);
        var running = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == staleRunning.Id, TestContext.Current.CancellationToken);
        var fresh = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == freshRunning.Id, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), pending.Status);
        Assert.Contains("Pending", pending.ErrorMessage, StringComparison.Ordinal);
        Assert.NotNull(pending.FinishedTimestamp);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), running.Status);
        Assert.Contains("stuck-run recovery", running.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(nameof(ReportGenerationStatus.Running), fresh.Status);
    }

    [Fact]
    public async Task Stuck_Recovery_IsDisabledWhenNotConfigured()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var stale = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2));
        db.ReportGenerations.Add(stale);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateRetentionService(new() { ConnectionString = "unused", StuckGenerationTimeout = null });
        Assert.Equal(0, await service.RecoverStuckGenerationsAsync(TestContext.Current.CancellationToken));
        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == stale.Id, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(ReportGenerationStatus.Running), row.Status);
    }

    [Fact]
    public async Task Stuck_RecoverySkipsAGenerationThat_IsStillExecuting()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var live = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-3));
        var abandoned = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-3));
        db.ReportGenerations.AddRange(live, abandoned);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Age alone once was enough to fail a row, so a long provider pass looked identical to a host that crashed mid-generation.
        var tracker = new ReportGenerationLiveTracker();
        using var _ = tracker.Track(live.Id);
        var service = CreateRetentionService(new() { ConnectionString = "unused", StuckGenerationTimeout = TimeSpan.FromHours(1) }, tracker);
        await service.RecoverStuckGenerationsAsync(TestContext.Current.CancellationToken);

        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var liveRow = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == live.Id, TestContext.Current.CancellationToken);
        var abandonedRow = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == abandoned.Id, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(ReportGenerationStatus.Running), liveRow.Status);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), abandonedRow.Status);
    }

    /// <summary>A <c>GenerationTimeout</c> longer than the stuck timeout has to win, otherwise recovery fails runs that are still inside their allowed wall clock.</summary>
    [Fact]
    public async Task Stuck_RecoveryDefersToALongerGeneration_Timeout()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var running = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-2));
        db.ReportGenerations.Add(running);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateRetentionService(
            new() { ConnectionString = "unused", StuckGenerationTimeout = TimeSpan.FromHours(1), GenerationTimeout = TimeSpan.FromHours(6) });

        await service.RecoverStuckGenerationsAsync(TestContext.Current.CancellationToken);
        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await verify.ReportGenerations.AsNoTracking().FirstAsync(g => g.Id == running.Id, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(ReportGenerationStatus.Running), row.Status);
    }

    [Fact]
    public async Task Retention_CleanupContinuesPastFailingCleanup_Hook()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        // Oldest row's cleanup hook fails forever; the two newer rows must still be deleted.
        var poison = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow.AddDays(-40));
        var newer1 = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow.AddDays(-30));
        var newer2 = NewGeneration(nameof(ReportGenerationStatus.Failed), DateTime.UtcNow.AddDays(-20));
        db.ReportGenerations.AddRange(poison, newer1, newer2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var hooks = new ReportGenerationHooks { OnCleanupAsync = (ctx, _) => ctx.GenerationId == poison.Id ? throw new IOException("blob is gone") : ValueTask.CompletedTask };
        var service = CreateRetentionService(new() { ConnectionString = "unused", GenerationRetention = TimeSpan.FromDays(7), StuckGenerationTimeout = null });
        var deleted = await service.CleanupAsync(hooks, TestContext.Current.CancellationToken);
        Assert.True(deleted >= 2, $"Expected the two healthy rows to be deleted despite the poison row, got {deleted}.");
        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.True(await verify.ReportGenerations.AnyAsync(g => g.Id == poison.Id, TestContext.Current.CancellationToken), "Row with failing hook must be retained.");
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == newer1.Id, TestContext.Current.CancellationToken));
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == newer2.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Generate_TimesOutAndPersistsFailedWithTimeout_Message()
    {
        using var scope = fixture.CreateScope();
        var reportService = CreateReportService(scope.ServiceProvider, new() { ConnectionString = "unused", GenerationTimeout = TimeSpan.FromMilliseconds(200) });
        var hooks = new ReportGenerationHooks { BeforeGenerateAsync = async (_, ct) => await Task.Delay(TimeSpan.FromSeconds(30), ct) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv, CreatedBy = "timeout-user" }, hooks, TestContext.Current.CancellationToken));

        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var failed = await db.ReportGenerations.AsNoTracking().FirstOrDefaultAsync(g => g.CreatedBy == "timeout-user", TestContext.Current.CancellationToken);
        Assert.NotNull(failed);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), failed!.Status);
        Assert.Contains("timed out", failed.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_FailurePersistsFailedGeneration_Row()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "Provider Boom",
            ReportDataJson = SampleReportJson(),
            IsActive = true,
            GenerationProfileKey = "boom-profile",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var reportService = CreateReportService(scope.ServiceProvider, new() { ConnectionString = "unused" }, [new ThrowingProvider()]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv, CreatedBy = "provider-user" }, ct: TestContext.Current.CancellationToken));

        await using var verify = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var failed = await verify.ReportGenerations.AsNoTracking().FirstOrDefaultAsync(g => g.CreatedBy == "provider-user", TestContext.Current.CancellationToken);
        Assert.NotNull(failed);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), failed!.Status);
        Assert.Contains("provider boom", failed.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_GenerationKeepsUploadedOutputFile_Id()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var uploadedFileId = Guid.NewGuid();
        var hooks = new ReportGenerationHooks {
            AfterSaveAsync = (ctx, _) => {
                // Simulate the host uploading the blob, then failing before generate completes.
                ctx.OutputFileId = uploadedFileId;
                throw new IOException("post-upload failure");
            }
        };

        await Assert.ThrowsAsync<IOException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv, CreatedBy = "orphan-user" }, hooks, TestContext.Current.CancellationToken));

        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var failed = await db.ReportGenerations.AsNoTracking().FirstOrDefaultAsync(g => g.CreatedBy == "orphan-user", TestContext.Current.CancellationToken);
        Assert.NotNull(failed);
        Assert.Equal(nameof(ReportGenerationStatus.Failed), failed!.Status);
        Assert.Equal(uploadedFileId, failed.OutputFileId);
    }

    [Fact]
    public async Task Duplicate_DefinitionParameterKeysRejectedBy_Database()
    {
        await using var db = await DbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definitionId = Guid.NewGuid();
        db.ReportDefinitions.Add(
            new() {
                Id = definitionId,
                Name = "Unique Keys",
                ReportDataJson = "{}",
                IsActive = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        db.ReportDefinitionParameters.AddRange(NewParameter(definitionId, "ClientId"), NewParameter(definitionId, "ClientId"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private static ReportGeneration NewGeneration(string status, DateTime created, DateTime? started = null)
        => new() {
            Id = Guid.NewGuid(),
            ReportDataJson = "{}",
            Format = nameof(ReportFormat.Csv),
            Status = status,
            CreatedBy = "resilience-test",
            CreatedTimestamp = created,
            StartedTimestamp = started
        };

    private static ReportDefinitionParameter NewParameter(Guid definitionId, string key)
        => new() {
            Id = Guid.NewGuid(),
            ReportDefinitionId = definitionId,
            Key = key,
            Type = LyoTypeInfo.String.FullName,
            Required = false,
            CreatedTimestamp = DateTime.UtcNow
        };

    private sealed class ThrowingProvider : IReportDataProvider
    {
        public string ProfileKey => "boom-profile";

        public Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default) => throw new InvalidOperationException("provider boom");
    }
}