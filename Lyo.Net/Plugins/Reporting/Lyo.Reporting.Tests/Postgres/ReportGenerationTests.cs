using Lyo.FileStorage.Abstractions;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests.Postgres;

[Trait("Category", "Integration")]
public sealed class ReportGenerationTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    [Fact]
    public async Task Generate_CsvPersistsViaConsumer_Hook()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("People")
            .AddSection("Data", s => s.AddTable("People", t => t.AddColumn("Name").AddColumn("Age").AddRow("Ada", 36).AddRow("Grace", 40)))
            .Build();

        var json = ReportJson.Serialize(report);
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "People Report",
            ReportDataJson = json,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var hooks = new ReportGenerationHooks {
            AfterRenderAsync = async (ctx, ct) => {
                var storage = ctx.Services.GetRequiredService<IFileStorageService>();
                var saved = await storage.SaveFileAsync(
                        ctx.StagedFilePath!, ctx.FileName, pathPrefix: ctx.PathPrefix ?? ctx.Request.PathPrefix, contentType: ctx.ContentType, ct: ct)
                    .ConfigureAwait(false);

                ctx.OutputFileId = saved.Id;
            }
        };

        var result = await reportService.GenerateAsync(
            new() {
                ReportDefinitionId = definition.Id,
                Format = ReportFormat.Csv,
                FileName = "people.csv",
                PathPrefix = "reports/test"
            }, hooks, TestContext.Current.CancellationToken);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.NotNull(result.OutputFileId);
        Assert.True(fixture.FakeFileStorage.Files.ContainsKey(result.OutputFileId!.Value));
        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.Equal("people.csv", result.OriginalFileName);
    }

    [Fact]
    public async Task Generate_AdhocWithoutHooks_LeavesOutputFileIdNull()
    {
        var report = ReportBuilder<object>.New().SetTitle("Adhoc").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddRow("1"))).Build();
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        var result = await reportService.GenerateAsync(
            new() { ReportDataJson = ReportJson.Serialize(report), Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Null(result.OutputFileId);
        Assert.Null(result.ReportDefinitionId);
    }
}