using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Models;
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

[Trait("Category", "Integration")]
public sealed class ReportGenerationHardenTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    [Fact]
    public async Task Generate_Rejects_InactiveDefinition()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "Inactive",
            ReportDataJson = "{}",
            IsActive = false,
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv }, ct: TestCt));
    }

    [Fact]
    public async Task Generate_PersistsFailedStatusThen_Rethrows()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<InvalidOperationException>(()
            => reportService.GenerateAsync(new() { ReportDataJson = """{"Title":"x"}""", Format = ReportFormat.Pdf }, ct: TestCt));

        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var failed = await db.ReportGenerations.AsNoTracking()
            .Where(g => g.Status == nameof(ReportGenerationStatus.Failed))
            .OrderByDescending(g => g.CreatedTimestamp)
            .FirstOrDefaultAsync(TestCt);

        Assert.NotNull(failed);
        Assert.False(string.IsNullOrWhiteSpace(failed!.ErrorMessage));
        Assert.Equal("Unknown", failed.CreatedBy);
    }

    [Fact]
    public async Task Generate_Uses_ProviderAndProfileDefaults()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "With Provider",
            ReportDataJson = ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("Seed").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddRow("1"))).Build()),
            IsActive = true,
            GenerationProfileKey = "stub-profile",
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var sp = scope.ServiceProvider;
        var reportService = new ReportService(
            sp.GetRequiredService<IDbContextFactory<ReportingContext>>(), sp.GetServices<IReportRenderer>(), [new StubProvider()],
            [new() { Key = "stub-profile", DefaultFormat = ReportFormat.Csv, DefaultFileName = "from-profile.csv" }], sp, sp.GetRequiredService<PostgresReportingOptions>(),
            sp.GetRequiredService<ILogger<ReportService>>());

        var result = await reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, CreatedBy = "worker-a", IncludeReportData = true }, ct: TestCt);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal(ReportFormat.Csv, result.Format);
        Assert.Equal("from-profile.csv", result.OriginalFileName);
        Assert.Equal("worker-a", result.CreatedBy);
        Assert.Contains("provider-row", result.ReportDataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_DefinitionCascades_Generations()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definition = new ReportDefinition {
            Id = LyoGuid.CreateV7(),
            Name = "Cascade",
            ReportDataJson = "{}",
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        var generation = new ReportGeneration {
            Id = LyoGuid.CreateV7(),
            ReportDefinitionId = definition.Id,
            ReportDataJson = "{}",
            Format = nameof(ReportFormat.Csv),
            Status = nameof(ReportGenerationStatus.Succeeded),
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(TestCt);
        db.ReportDefinitions.Remove(definition);
        await db.SaveChangesAsync(TestCt);
        Assert.False(await db.ReportGenerations.AnyAsync(g => g.Id == generation.Id, TestCt));
    }

    [Fact]
    public async Task Generate_MergesValidatesAndPersists_Parameters()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definitionId = LyoGuid.CreateV7();
        var definition = new ReportDefinition {
            Id = definitionId,
            Name = "With Params",
            ReportDataJson = ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("P").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddRow("1"))).Build()),
            IsActive = true,
            DefaultFormat = nameof(ReportFormat.Csv),
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow,
            Parameters = [
                new() {
                    Id = LyoGuid.CreateV7(),
                    ReportDefinitionId = definitionId,
                    Key = "ClientId",
                    Type = LyoTypeInfo.String.FullName,
                    Value = "default-client",
                    Required = true,
                    CreatedTimestamp = DateTime.UtcNow
                }
            ]
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestCt);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definitionId, Parameters = [new("ClientId", LyoTypeInfo.String, "")] }, ct: TestCt));

        var result = await reportService.GenerateAsync(
            new() { ReportDefinitionId = definitionId, Parameters = [new("ClientId", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("override-client"))], CreatedBy = "worker-p" },
            ct: TestCt);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.NotNull(result.Parameters);
        Assert.Single(result.Parameters!);
        Assert.Equal(LyoTypeInfo.String.ToJson("override-client"), result.Parameters![0].Value);
        await using var verify = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var stored = await verify.ReportGenerationParameters.AsNoTracking().Where(p => p.ReportGenerationId == result.Id).ToListAsync(TestCt);
        Assert.Single(stored);
        Assert.Equal(LyoTypeInfo.String.ToJson("override-client"), stored[0].Value);
    }

    [Fact]
    public async Task Delete_DefinitionCascadesDefinition_Parameters()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestCt);
        var definitionId = LyoGuid.CreateV7();
        var paramId = LyoGuid.CreateV7();
        db.ReportDefinitions.Add(
            new() {
                Id = definitionId,
                Name = "Cascade Params",
                ReportDataJson = "{}",
                IsActive = true,
                CreatedTimestamp = DateTime.UtcNow,
                Parameters = [
                    new() {
                        Id = paramId,
                        ReportDefinitionId = definitionId,
                        Key = "X",
                        Type = LyoTypeInfo.String.FullName,
                        Required = false,
                        CreatedTimestamp = DateTime.UtcNow
                    }
                ]
            });

        await db.SaveChangesAsync(TestCt);
        var def = await db.ReportDefinitions.Include(d => d.Parameters).FirstAsync(d => d.Id == definitionId, TestCt);
        db.ReportDefinitions.Remove(def);
        await db.SaveChangesAsync(TestCt);
        Assert.False(await db.ReportDefinitionParameters.AnyAsync(p => p.Id == paramId, TestCt));
    }

    private sealed class StubProvider : IReportDataProvider
    {
        public string ProfileKey => "stub-profile";

        public Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default)
        {
            var report = ReportBuilder<object>.New().SetTitle("FromProvider").AddSection(s => s.AddTable("G", t => t.AddColumn("A").AddRow("provider-row"))).Build();
            return Task.FromResult(new ReportDataProviderResult { ReportDataJson = ReportJson.Serialize(report) });
        }
    }
}