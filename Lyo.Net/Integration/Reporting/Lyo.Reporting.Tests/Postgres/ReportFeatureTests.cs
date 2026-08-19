using System.Text.Json;
using Lyo.Api.Reporting;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Xlsx.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Tests.Postgres;

/// <summary>Integration coverage for new reporting features: Xlsx/Json formats, rerun, retention cleanup, and input hygiene.</summary>
[Trait("Category", "Integration")]
public sealed class ReportFeatureTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    private static string SampleReportJson()
        => JsonSerializer.Serialize(ReportBuilder<object>.New().SetTitle("Sample").AddSection(s => s.AddGrid("G", g => g.AddColumn("A").AddColumn("B").AddRow("1", "2"))).Build());

    private ReportService CreateService(IServiceProvider sp, PostgresReportingOptions options)
        => new(
            sp.GetRequiredService<IDbContextFactory<ReportingContext>>(), sp.GetServices<IReportRenderer>(), [], [], sp, Options.Create(options),
            sp.GetRequiredService<ILogger<ReportService>>());

    [Fact]
    public async Task Generate_xlsx_produces_valid_workbook_output()
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

        var result = await reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Xlsx, FileName = "sample.xlsx" }, hooks, TestContext.Current.CancellationToken);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.Equal("sample.xlsx", result.OriginalFileName);
        Assert.NotNull(staged);
        var xlsx = scope.ServiceProvider.GetRequiredService<IXlsxService>();
        // Reader consumes the header row by default, so index 0 is the first data row.
        var parsed = xlsx.Reader.ParseXlsxBytesAsDictionary(staged!);
        Assert.Equal("1", parsed[0][0]);
        Assert.Equal("2", parsed[0][1]);
    }

    [Fact]
    public async Task Generate_json_writes_composition_json()
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
        var result = await reportService.GenerateAsync(new() { ReportDataJson = json, Format = ReportFormat.Json }, hooks, TestContext.Current.CancellationToken);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("application/json; charset=utf-8", result.ContentType);
        Assert.Equal(json, stagedText);
    }

    [Fact]
    public async Task Generate_sanitizes_traversal_and_invalid_chars_in_file_name()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var result = await reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv, FileName = "../../etc/we\0ird.csv" }, ct: TestContext.Current.CancellationToken);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal("weird.csv", result.OriginalFileName);
    }

    [Fact]
    public async Task Generate_rejects_malformed_report_data_json()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = "{definitely not json", Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken));

        Assert.Contains("not valid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(
            await db.ReportGenerations.AnyAsync(g => g.ReportDataJson == "{definitely not json", TestContext.Current.CancellationToken),
            "Malformed payloads must fail fast, before a generation row is persisted.");
    }

    [Fact]
    public async Task Generate_rejects_adhoc_when_disabled()
    {
        using var scope = fixture.CreateScope();
        var options = new PostgresReportingOptions { ConnectionString = "unused", AllowAdHocGeneration = false };
        var reportService = CreateService(scope.ServiceProvider, options);
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = SampleReportJson(), Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken));

        Assert.Contains("AllowAdHocGeneration", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_rejects_override_json_on_definition_when_adhoc_disabled()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "NoAdhoc",
            ReportDataJson = SampleReportJson(),
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var options = new PostgresReportingOptions { ConnectionString = "unused", AllowAdHocGeneration = false };
        var reportService = CreateService(scope.ServiceProvider, options);
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, OverrideReportDataJson = SampleReportJson(), Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken));

        // Generating from the stored definition JSON is still allowed.
        var ok = await reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken);
        Assert.Equal(ReportGenerationStatus.Succeeded, ok.Status);
    }

    [Fact]
    public async Task Generate_rejects_unknown_parameter_keys_for_definitions()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "StrictKeys",
            ReportDataJson = SampleReportJson(),
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv, Parameters = [new("NotDeclared", ReportParameterType.String, "x")] },
            ct: TestContext.Current.CancellationToken));

        Assert.Contains("NotDeclared", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rerun_reproduces_snapshot_into_new_generation()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var original = await reportService.GenerateAsync(
            new() {
                ReportDataJson = SampleReportJson(),
                Format = ReportFormat.Csv,
                FileName = "snapshot.csv",
                Parameters = [new("Tag", ReportParameterType.String, "keep-me")],
                CreatedBy = "original-user"
            }, ct: TestContext.Current.CancellationToken);

        var rerun = await reportService.RerunAsync(original.Id, "rerun-user", ct: TestContext.Current.CancellationToken);
        Assert.NotEqual(original.Id, rerun.Id);
        Assert.Equal(ReportGenerationStatus.Succeeded, rerun.Status);
        Assert.Equal(ReportFormat.Csv, rerun.Format);
        Assert.Equal("snapshot.csv", rerun.OriginalFileName);
        Assert.Equal("rerun-user", rerun.CreatedBy);
        Assert.Equal(original.ReportDataJson, rerun.ReportDataJson);
        Assert.NotNull(rerun.Parameters);
        Assert.Contains(rerun.Parameters!, p => p is { Key: "Tag", Value: "keep-me" });
    }

    [Fact]
    public async Task Rerun_of_missing_generation_is_a_validation_error()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.RerunAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Retention_cleanup_deletes_only_old_terminal_rows_and_invokes_hook()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var outputFileId = Guid.NewGuid();
        fixture.FakeFileStorage.Files[outputFileId] = [1, 2, 3];
        var oldWithFile = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow.AddDays(-30), outputFileId);
        var oldFailed = NewGeneration(nameof(ReportGenerationStatus.Failed), DateTime.UtcNow.AddDays(-30));
        var oldRunning = NewGeneration(nameof(ReportGenerationStatus.Running), DateTime.UtcNow.AddDays(-30));
        var recent = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow);
        db.ReportGenerations.AddRange(oldWithFile, oldFailed, oldRunning, recent);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cleanupCalls = new List<Guid?>();
        var hooks = new ReportGenerationHooks {
            OnCleanupAsync = async (ctx, ct) => {
                cleanupCalls.Add(ctx.OutputFileId);
                if (ctx.OutputFileId is Guid fileId)
                    await fixture.FakeFileStorage.DeleteFileAsync(fileId, ct: ct);
            }
        };

        var service = new ReportRetentionService(
            dbFactory, fixture.ServiceProvider, Options.Create(new PostgresReportingOptions { ConnectionString = "unused", GenerationRetention = TimeSpan.FromDays(7) }),
            fixture.ServiceProvider.GetRequiredService<ILogger<ReportRetentionService>>());

        var deleted = await service.CleanupAsync(hooks, TestContext.Current.CancellationToken);
        Assert.True(deleted >= 2, $"Expected at least the two old terminal rows to be deleted, got {deleted}.");
        Assert.Contains(outputFileId, cleanupCalls);
        Assert.False(fixture.FakeFileStorage.Files.ContainsKey(outputFileId));
        await using var verify = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == oldWithFile.Id, TestContext.Current.CancellationToken));
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == oldFailed.Id, TestContext.Current.CancellationToken));
        Assert.True(await verify.ReportGenerations.AnyAsync(g => g.Id == oldRunning.Id, TestContext.Current.CancellationToken), "In-flight rows must be kept.");
        Assert.True(await verify.ReportGenerations.AnyAsync(g => g.Id == recent.Id, TestContext.Current.CancellationToken), "Recent rows must be kept.");
    }

    [Fact]
    public async Task Retention_cleanup_is_disabled_without_configuration()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        var service = new ReportRetentionService(
            dbFactory, fixture.ServiceProvider, Options.Create(new PostgresReportingOptions { ConnectionString = "unused" }),
            fixture.ServiceProvider.GetRequiredService<ILogger<ReportRetentionService>>());

        Assert.Equal(0, await service.CleanupAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Download_factory_streams_persisted_output_via_fake_storage()
    {
        // Exercises the host-pluggable contract used by the Download endpoint: factory resolves the blob by OutputFileId.
        var fileId = Guid.NewGuid();
        fixture.FakeFileStorage.Files[fileId] = [9, 8, 7];
        Func<ReportDownloadContext, CancellationToken, Task<Stream?>> factory = (ctx, ct) => fixture.FakeFileStorage.GetFileStreamAsync(ctx.OutputFileId, ct: ct);
        var context = new ReportDownloadContext {
            GenerationId = Guid.NewGuid(),
            OutputFileId = fileId,
            ContentType = "text/csv",
            FileName = "x.csv",
            Services = fixture.ServiceProvider
        };

        await using var stream = await factory(context, TestContext.Current.CancellationToken);
        Assert.NotNull(stream);
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms, TestContext.Current.CancellationToken);
        Assert.Equal([9, 8, 7], ms.ToArray());
        var missingContext = new ReportDownloadContext { GenerationId = Guid.NewGuid(), OutputFileId = Guid.NewGuid(), Services = fixture.ServiceProvider };
        Assert.Null(await factory(missingContext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_generation_invokes_cleanup_hook_and_removes_stored_file()
    {
        var dbFactory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var outputFileId = Guid.NewGuid();
        fixture.FakeFileStorage.Files[outputFileId] = [1, 2, 3];
        var generation = NewGeneration(nameof(ReportGenerationStatus.Succeeded), DateTime.UtcNow, outputFileId);
        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var hooks = new ReportGenerationHooks {
            OnCleanupAsync = async (ctx, ct) => {
                if (ctx.OutputFileId is Guid fileId)
                    await fixture.FakeFileStorage.DeleteFileAsync(fileId, ct: ct);
            }
        };

        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<ReportingContext>>();
        var result = await delete.DeleteAsync<ReportGeneration, ReportGenerationRes>(
            [generation.Id], beforeAsync: (ctx, ct) => ReportGenerationCleanup.InvokeCleanupHooksAsync([ctx.Entity], hooks, ctx.Services, ct).AsTask(),
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(fixture.FakeFileStorage.Files.ContainsKey(outputFileId));
        await using var verify = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await verify.ReportGenerations.AnyAsync(g => g.Id == generation.Id, TestContext.Current.CancellationToken));
    }

    private static ReportGeneration NewGeneration(string status, DateTime created, Guid? outputFileId = null)
        => new() {
            Id = Guid.NewGuid(),
            ReportDataJson = "{}",
            Format = nameof(ReportFormat.Csv),
            Status = status,
            OutputFileId = outputFileId,
            CreatedBy = "retention-test",
            CreatedTimestamp = created
        };
}