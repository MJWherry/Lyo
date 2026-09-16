using Lyo.FileSystemWatcher.Postgres.Database;
using Lyo.FileSystemWatcher.Seed;
using Lyo.Seed;

namespace Lyo.FileSystemWatcher.Seed.Tests;

public sealed class FileSystemWatcherEfSeedContributorTests
{
    [Fact]
    public async Task FileSystemWatcherEfSeedContributor_Count_PersistsWatchSnapshotAndChange()
    {
        var transport = await SeedAsync(new FileSystemWatcherEfSeedContributor(), 2);
        var watches = transport.Items.OfType<FileSystemWatchEntity>().ToList();
        Assert.Equal(2, watches.Count);
        Assert.All(watches, watch => {
            var snapshot = Assert.Single(transport.Items.OfType<FileSystemSnapshotEntity>(), row => row.WatchId == watch.Id);
            Assert.Equal(64, snapshot.ContentHash.Length);
            var change = Assert.Single(transport.Items.OfType<FileSystemChangeEntity>(), row => row.WatchId == watch.Id);
            Assert.Equal(snapshot.Id, change.SnapshotId);
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
