using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Seed;
using Lyo.Seed;

namespace Lyo.Reporting.Seed.Tests;

public sealed class ReportingEfSeedContributorTests
{
    [Fact]
    public async Task ReportingEfSeedContributor_Samples_WritesTemplateDefinitions()
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(new ReportingEfSeedContributor(), transport, new() { Count = 99, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        var definitions = transport.Items.OfType<ReportDefinition>().ToList();
        Assert.Equal(ReportDesignTemplates.Samples.Count, definitions.Count);
        Assert.NotEqual(99, definitions.Count);
        Assert.Equal(ReportDesignTemplates.Samples.Select(s => s.Name), definitions.Select(d => d.Name));
        var generations = transport.Items.OfType<ReportGeneration>().ToList();
        var generationParameters = transport.Items.OfType<ReportGenerationParameter>().ToList();
        Assert.Equal(definitions.Count, generations.Count);
        foreach (var (sample, definition) in ReportDesignTemplates.Samples.Zip(definitions)) {
            var req = sample.ToDefinitionReq();
            Assert.Equal(req.ReportDataJson, definition.ReportDataJson);
            Assert.Equal(req.CreateParameters.Select(p => p.Key), definition.Parameters.Select(p => p.Key));
            var generation = Assert.Single(generations, row => row.ReportDefinitionId == definition.Id);
            Assert.Equal(definition.ReportDataJson, generation.ReportDataJson);
            Assert.Equal(nameof(ReportGenerationStatus.Succeeded), generation.Status);
            var parameters = generationParameters.Where(row => row.ReportGenerationId == generation.Id).ToList();
            Assert.Equal(definition.Parameters.Select(p => p.Key), parameters.Select(p => p.Key));
        }
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
