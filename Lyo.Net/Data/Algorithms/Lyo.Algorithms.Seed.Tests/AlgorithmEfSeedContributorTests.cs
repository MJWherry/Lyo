using Lyo.Algorithms.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Algorithms.Seed.Tests;

public sealed class AlgorithmEfSeedContributorTests
{
    [Fact]
    public async Task AlgorithmEfSeedContributor_Count_PersistsDefinitionRevisionAndLabel()
    {
        var transport = await SeedAsync(new AlgorithmEfSeedContributor(), 2);
        var definitions = transport.Items.OfType<AlgorithmDefinitionEntity>().ToList();
        var revisions = transport.Items.OfType<AlgorithmRevisionEntity>().ToList();
        var labels = transport.Items.OfType<AlgorithmLabelEntity>().ToList();
        Assert.Equal(2, definitions.Count);
        Assert.Equal(2, revisions.Count);
        Assert.Equal(2, labels.Count);
        Assert.DoesNotContain(transport.Items, item => item.GetType().Name.Contains("Board"));
        var fishing = Assert.Single(definitions, row => row.Key == AlgorithmEfSeedContributor.FishingKey);
        var revision = Assert.Single(revisions, row => row.DefinitionId == fishing.Id);
        Assert.Equal(1, revision.Revision);
        Assert.Equal(AlgorithmEfSeedContributor.FishingFormula, revision.Source);
        Assert.Contains(labels, row => row.DefinitionId == fishing.Id && row.Label == "latest" && row.Revision == 1);
        Assert.All(
            definitions, definition => {
                Assert.Contains(revisions, row => row.DefinitionId == definition.Id);
                Assert.Contains(labels, row => row.DefinitionId == definition.Id);
            });
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
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