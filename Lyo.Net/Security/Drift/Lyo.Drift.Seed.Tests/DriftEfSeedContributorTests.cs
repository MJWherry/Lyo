using Lyo.Drift.Postgres.Database;
using Lyo.Drift.Seed;
using Lyo.Seed;

namespace Lyo.Drift.Seed.Tests;

public sealed class DriftEfSeedContributorTests
{
    [Fact]
    public async Task DriftEfSeedContributor_Count_PersistsInstanceGraph()
    {
        var transport = await SeedAsync(new DriftEfSeedContributor(), 2);
        var instances = transport.Items.OfType<DriftInstance>().ToList();
        Assert.Equal(2, instances.Count);
        Assert.Equal(2, instances.Select(row => row.MachineName).Distinct().Count());
        Assert.DoesNotContain(instances, row => row.MachineName == "seed");
        Assert.All(instances, instance => {
            var snapshot = Assert.Single(transport.Items.OfType<DriftStructureSnapshot>(), row => row.InstanceId == instance.Id);
            Assert.Equal(64, snapshot.ContentHash.Length);
            Assert.Contains(transport.Items.OfType<DriftDiffSnapshot>(), row => row.InstanceId == instance.Id && row.ToSnapshotId == snapshot.Id);
            Assert.Contains(transport.Items.OfType<DriftChangeEvent>(), row => row.InstanceId == instance.Id && row.SnapshotId == snapshot.Id);
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
        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default) where T : class => Task.FromResult(!Items.OfType<T>().Any());
        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default) where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
