using System.Text.Json;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Tests.Postgres;

[Trait("Category", "Integration")]
public sealed class ReportGenerationHardenTests(ReportingPostgresFixture fixture) : IClassFixture<ReportingPostgresFixture>
{
    [Fact]
    public async Task Generate_rejects_inactive_definition()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            ReportDataJson = "{}",
            IsActive = false,
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definition.Id, Format = ReportFormat.Csv }, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Generate_persists_failed_status_then_rethrows()
    {
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => reportService.GenerateAsync(
            new() { ReportDataJson = """{"Title":"x"}""", Format = ReportFormat.Pdf }, ct: TestContext.Current.CancellationToken));

        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var failed = await db.ReportGenerations.AsNoTracking()
            .Where(g => g.Status == nameof(ReportGenerationStatus.Failed))
            .OrderByDescending(g => g.CreatedTimestamp)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(failed);
        Assert.False(string.IsNullOrWhiteSpace(failed!.ErrorMessage));
        Assert.Equal("Unknown", failed.CreatedBy);
    }

    [Fact]
    public async Task Generate_uses_provider_and_profile_defaults()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "With Provider",
            ReportDataJson = JsonSerializer.Serialize(ReportBuilder<object>.New().SetTitle("Seed").AddSection(s => s.AddGrid("G", g => g.AddColumn("A").AddRow("1"))).Build()),
            IsActive = true,
            GenerationProfileKey = "stub-profile",
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var sp = scope.ServiceProvider;
        var reportService = new ReportService(
            sp.GetRequiredService<IDbContextFactory<ReportingContext>>(), sp.GetServices<IReportRenderer>(), [new StubProvider()],
            [new() { Key = "stub-profile", DefaultFormat = ReportFormat.Csv, DefaultFileName = "from-profile.csv" }], sp,
            sp.GetRequiredService<IOptions<PostgresReportingOptions>>(), sp.GetRequiredService<ILogger<ReportService>>());

        var result = await reportService.GenerateAsync(new() { ReportDefinitionId = definition.Id, CreatedBy = "worker-a" }, ct: TestContext.Current.CancellationToken);
        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.Equal(ReportFormat.Csv, result.Format);
        Assert.Equal("from-profile.csv", result.OriginalFileName);
        Assert.Equal("worker-a", result.CreatedBy);
        Assert.Contains("provider-row", result.ReportDataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_definition_cascades_generations()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definition = new ReportDefinition {
            Id = Guid.NewGuid(),
            Name = "Cascade",
            ReportDataJson = "{}",
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        var generation = new ReportGeneration {
            Id = Guid.NewGuid(),
            ReportDefinitionId = definition.Id,
            ReportDataJson = "{}",
            Format = nameof(ReportFormat.Csv),
            Status = nameof(ReportGenerationStatus.Succeeded),
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow
        };

        db.ReportDefinitions.Add(definition);
        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ReportDefinitions.Remove(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.False(await db.ReportGenerations.AnyAsync(g => g.Id == generation.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Generate_merges_validates_and_persists_parameters()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definitionId = Guid.NewGuid();
        var definition = new ReportDefinition {
            Id = definitionId,
            Name = "With Params",
            ReportDataJson = JsonSerializer.Serialize(ReportBuilder<object>.New().SetTitle("P").AddSection(s => s.AddGrid("G", g => g.AddColumn("A").AddRow("1"))).Build()),
            IsActive = true,
            DefaultFormat = nameof(ReportFormat.Csv),
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow,
            Parameters = [
                new() {
                    Id = Guid.NewGuid(),
                    ReportDefinitionId = definitionId,
                    Key = "ClientId",
                    Type = nameof(ReportParameterType.String),
                    Value = "default-client",
                    Required = true,
                    CreatedTimestamp = DateTime.UtcNow
                }
            ]
        };

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
        await Assert.ThrowsAsync<ReportValidationException>(() => reportService.GenerateAsync(
            new() { ReportDefinitionId = definitionId, Parameters = [new("ClientId", ReportParameterType.String, "")] }, ct: TestContext.Current.CancellationToken));

        var result = await reportService.GenerateAsync(
            new() { ReportDefinitionId = definitionId, Parameters = [new("ClientId", ReportParameterType.String, "override-client")], CreatedBy = "worker-p" },
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(ReportGenerationStatus.Succeeded, result.Status);
        Assert.NotNull(result.Parameters);
        Assert.Single(result.Parameters!);
        Assert.Equal("override-client", result.Parameters![0].Value);
        await using var verify = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);

        var stored = await verify.ReportGenerationParameters.AsNoTracking().Where(p => p.ReportGenerationId == result.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(stored);
        Assert.Equal("override-client", stored[0].Value);
    }

    [Fact]
    public async Task Delete_definition_cascades_definition_parameters()
    {
        await using var db = await fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var definitionId = Guid.NewGuid();
        var paramId = Guid.NewGuid();
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
                        Type = nameof(ReportParameterType.String),
                        Required = false,
                        CreatedTimestamp = DateTime.UtcNow
                    }
                ]
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var def = await db.ReportDefinitions.Include(d => d.Parameters).FirstAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken);
        db.ReportDefinitions.Remove(def);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.False(await db.ReportDefinitionParameters.AnyAsync(p => p.Id == paramId, TestContext.Current.CancellationToken));
    }

    private sealed class StubProvider : IReportDataProvider
    {
        public string ProfileKey => "stub-profile";

        public Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default)
        {
            var report = ReportBuilder<object>.New().SetTitle("FromProvider").AddSection(s => s.AddGrid("G", g => g.AddColumn("A").AddRow("provider-row"))).Build();
            return Task.FromResult(new ReportDataProviderResult { ReportDataJson = JsonSerializer.Serialize(report) });
        }
    }
}