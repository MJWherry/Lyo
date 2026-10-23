using Lyo.Api.Services.Crud.Delete;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Api;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Xlsx.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Reporting.Tests.Postgres;

/// <summary>Integration coverage for new reporting features: Xlsx and Json formats, rerun, retention cleanup, and input hygiene.</summary>
[Trait("Category", "Integration")]
public sealed class ReportFeatureTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    private static string SampleReportJson()
        => ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("Sample").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddColumn("B").AddRow("1", "2"))).Build());

    private ReportService CreateService(IServiceProvider sp, PostgresReportingOptions options)
        => new(
            sp.GetRequiredService<IDbContextFactory<ReportingContext>>(), sp.GetServices<IReportRenderer>(), [], [], sp, options, sp.GetRequiredService<ILogger<ReportService>>());

    [Fact]
    public async Task Generate_Xlsx_ProducesValidWorkbookOutput()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        byte[]? staged = null;
        var hooks = new ReportGenerationHooks {
            AfterRenderAsync = (ctx, _) => {
                staged = File.ReadAllBytes(ctx.StagedFilePath!);
                return ValueTask.CompletedTask;
            }
        };

        var result = await reportService.GenerateAsync(new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Xlsx, FileName = "sample.xlsx" }, hooks, TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.Equal("sample.xlsx", result.OriginalFileName);
        Assert.NotNull(staged);
        var xlsx = scope.ServiceProvider.GetRequiredService<IXlsxService>();
        // The reader consumes the header row by default, so index 0 is the first data row.
        var parsed = xlsx.Reader.ParseXlsxBytesAsDictionary(staged!);
        Assert.Equal("1", parsed[0][0]);
        Assert.Equal("2", parsed[0][1]);
    }

    [Fact]
    public async Task Generate_Json_WritesCompositionJson()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        string? stagedText = null;
        var hooks = new ReportGenerationHooks {
            AfterRenderAsync = (ctx, _) => {
                stagedText = File.ReadAllText(ctx.StagedFilePath!);
                return ValueTask.CompletedTask;
            }
        };

        var json = SampleReportJson();
        var result = await reportService.GenerateAsync(new() { ReportDataJson = json, Format = ReportFormat.Json }, hooks, TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("application/json; charset=utf-8", result.ContentType);
        Assert.Equal(json, stagedText);
    }

    [Fact]
    public async Task Generate_SanitizesTraversalAndInvalidCharsInFile_Name()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var result = await reportService.GenerateAsync(new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv, FileName = "../../etc/we\0ird.csv" }, ct: TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("weird.csv", result.OriginalFileName);
    }

    [Fact]
    public async Task Generate_Rejects_MalformedReportDataJson()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = "{definitely not json", Format = ReportFormat.Csv }, ct: TestCt));

        Assert.Contains("not valid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        Assert.False(
            await db.ReportGenerations.AnyAsync(g => g.ReportDataJson == "{definitely not json", TestCt),
            "Malformed payloads must fail fast, before a generation row is persisted.");
    }

    [Fact]
    public async Task Generate_Rejects_AdhocWhenDisabled()
    {
        using var scope = fixture.CreateScope();
        var options = new PostgresReportingOptions { ConnectionString = "unused", AllowAdHocGeneration = false };
        var reportService = CreateService(scope.ServiceProvider, options);
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv }, ct: TestCt));

        Assert.Contains("AllowAdHocGeneration", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_Rejects_OverrideJsonOnDefinitionWhenAdhocDisabled()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "NoAdhoc",
            ReportDataJson = SampleReportJson(),
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var options = new PostgresReportingOptions { ConnectionString = "unused", AllowAdHocGeneration = false };
        var reportService = CreateService(scope.ServiceProvider, options);
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, OverrideReportDataJson = SampleReportJson(), Format = ReportFormat.Csv }, ct: TestCt));

        // Generating from the stored definition JSON is still permitted.
        var ok = await reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv }, ct: TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, ok.Status);
    }

    [Fact]
    public async Task Generate_WithoutOverride_UsesStoredReportDataJson()
    {
        var stored = SampleReportJson();
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "StoredJson",
            ReportDataJson = stored,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var result = await reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Json, IncludeReportData = true }, ct: TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal(stored, result.ReportDataJson);
    }

    [Fact]
    public async Task Generate_Rejects_UnknownParameterKeysForDefinitions()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "StrictKeys",
            ReportDataJson = SampleReportJson(),
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv, Parameters = [new("NotDeclared", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("x"))] },
            ct: TestCt));

        Assert.Contains("NotDeclared", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rerun_ReproducesSnapshotIntoNew_Generation()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var original = await reportService.GenerateAsync(
            new() {
                ReportDataJson = SampleReportJson(),
                Format = ReportFormat.Csv,
                FileName = "snapshot.csv",
                Parameters = [new("Tag", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("keep-me"))],
                CreatedBy = "original-user",
                IncludeReportData = true
            }, ct: TestCt);

        var rerun = await reportService.RerunAsync(original.Id, "rerun-user", includeReportData: true, ct: TestCt);
        Assert.NotEqual(original.Id, rerun.Id);
        Assert.Equal(ReportGenerationStatus.Succeeded, rerun.Status);
        Assert.Equal(ReportFormat.Csv, rerun.Format);
        Assert.Equal("snapshot.csv", rerun.OriginalFileName);
        Assert.Equal("rerun-user", rerun.CreatedBy);
        Assert.NotNull(rerun.ReportDataJson);
        Assert.Equal(original.ReportDataJson, rerun.ReportDataJson);
        Assert.NotNull(rerun.Parameters);
        Assert.Contains(rerun.Parameters!, p => p.Key == "Tag" && p.Value == LyoTypeInfo.String.ToJson("keep-me"));
    }

    [Fact]
    public async Task Rerun_OfMissingGeneration_IsAValidationError()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.RerunAsync(LyoGuid.CreateV7(), ct: TestCt));
    }

    [Fact]
    public async Task Retention_CleanupDeletesOnlyOldTerminalRowsAndInvokes_Hook()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(TestCt);
        var outputFileId = LyoGuid.CreateV7();
        fixture.FakeFileStorage.Files[outputFileId] = [1, 2, 3];
        var oldWithFile = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow.AddDays(-30), outputFileId);
        var oldFailed = NewGeneration(nameof(ReportGenerationStatus.Failed), DateTime.UtcNow.AddDays(-30));
        var oldRunning = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddDays(-30));
        var recent = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow);
        db.ReportGenerations.AddRange(oldWithFile, oldFailed, oldRunning, recent);
        await db.SaveChangesAsync(TestCt);
        var cleanupCalls = new List<Guid?>();
        var hooks = new ReportGenerationHooks {
            OnCleanupAsync = async (ctx, ct) => {
                cleanupCalls.Add(ctx.OutputFileId);
                if (ctx.OutputFileId is Guid fileId)
                    await fixture.FakeFileStorage.DeleteFileAsync(fileId, ct: ct);
            }
        };

        var service = new ReportRetentionService(
            dbFactory, fixture.ServiceProvider, new() { ConnectionString = "unused", GenerationRetention = TimeSpan.FromDays(7) },
            fixture.ServiceProvider.GetRequiredService<ILogger<ReportRetentionService>>());

        var deleted = await service.CleanupAsync(hooks, TestCt);
        Assert.True(deleted >= 2, $"Expected at least the two old terminal rows to be deleted, got {deleted}.");
        Assert.Contains(outputFileId, cleanupCalls);
        Assert.False(fixture.FakeFileStorage.Files.ContainsKey(outputFileId));
        await using var verify = await dbFactory.CreateDbContextAsync(TestCt);
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == oldWithFile.Id, TestCt));
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == oldFailed.Id, TestCt));
        Assert.True(await verify.ReportGenerations.AnyAsync(g => g.Id == oldRunning.Id, TestCt), "In-flight rows must be kept.");
        Assert.True(await verify.ReportGenerations.AnyAsync(g => g.Id == recent.Id, TestCt), "Recent rows must be kept.");
    }

    [Fact]
    public async Task Retention_Cleanup_IsDisabledWithoutConfiguration()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        var service = new ReportRetentionService(
            dbFactory, fixture.ServiceProvider, new() { ConnectionString = "unused" }, fixture.ServiceProvider.GetRequiredService<ILogger<ReportRetentionService>>());

        Assert.Equal(0, await service.CleanupAsync(TestCt));
    }

    [Fact]
    public async Task Download_FactoryStreamsPersistedOutputViaFake_Storage()
    {
        // Exercises the host-pluggable contract used by the Download endpoint: the factory resolves the blob by OutputFileId.
        var fileId = LyoGuid.CreateV7();
        fixture.FakeFileStorage.Files[fileId] = [9, 8, 7];
        Func<ReportDownloadContext, CancellationToken, Task<Stream?>> factory = (ctx, ct) => fixture.FakeFileStorage.GetFileStreamAsync(ctx.OutputFileId, ct: ct);
        var context = new ReportDownloadContext {
            GenerationId = LyoGuid.CreateV7(),
            OutputFileId = fileId,
            ContentType = "text/csv",
            FileName = "x.csv",
            Services = fixture.ServiceProvider
        };

        await using var stream = await factory(context, TestCt);
        Assert.NotNull(stream);
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms, TestCt);
        Assert.Equal([9, 8, 7], ms.ToArray());
        var missingContext = new ReportDownloadContext { GenerationId = LyoGuid.CreateV7(), OutputFileId = LyoGuid.CreateV7(), Services = fixture.ServiceProvider };
        Assert.Null(await factory(missingContext, TestCt));
    }

    [Fact]
    public async Task Delete_GenerationInvokesCleanupHookAnd_RemovesStoredFile()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(TestCt);
        var outputFileId = LyoGuid.CreateV7();
        fixture.FakeFileStorage.Files[outputFileId] = [1, 2, 3];
        var generation = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow, outputFileId);
        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(TestCt);
        var hooks = new ReportGenerationHooks {
            OnCleanupAsync = async (ctx, ct) => {
                if (ctx.OutputFileId is Guid fileId)
                    await fixture.FakeFileStorage.DeleteFileAsync(fileId, ct: ct);
            }
        };

        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<ReportingContext>>();
        var result = await delete.DeleteAsync<ReportGeneration, ReportGenerationRes>(
            [generation.Id], beforeAsync: (ctx, ct) => ReportGenerationCleanup.InvokeCleanupHooksAsync([ctx.Entity], hooks, ctx.Services, ct).AsTask(), ct: TestCt);

        Assert.True(result.IsSuccess);
        Assert.False(fixture.FakeFileStorage.Files.ContainsKey(outputFileId));
        await using var verify = await dbFactory.CreateDbContextAsync(TestCt);
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == generation.Id, TestCt));
    }

    private static ReportGeneration NewGeneration(string status, DateTime created, Guid? outputFileId = null)
        => new() {
            Id = LyoGuid.CreateV7(),
            ReportDataJson = "{}",
            Format = nameof(ReportFormat.Csv),
            Status = status,
            OutputFileId = outputFileId,
            CreatedBy = "retention-test",
            CreatedTimestamp = created
        };
}