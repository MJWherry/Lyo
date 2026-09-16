using Lyo.ChangeTracker.Postgres.Database;
using Lyo.ChangeTracker.Seed;
using Lyo.Seed;

namespace Lyo.ChangeTracker.Seed.Tests;

public sealed class ChangeTrackerEfSeedContributorTests
{
    [Fact]
    public async Task ChangeTrackerEfSeedContributor_Count_PersistsChanges()
    {
        var transport = await SeedAsync(new ChangeTrackerEfSeedContributor(), 2);
        var changes = transport.Items.OfType<ChangeEntryEntity>().ToList();
        Assert.Equal(2, changes.Count);
        Assert.All(changes, change => {
            Assert.Equal("Updated", change.ChangeType);
            Assert.Contains("title", change.ChangedPropertiesJson);
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
