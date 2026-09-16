using Lyo.EntityReference.Models;
using Lyo.Favorite.Postgres.Database;
using Lyo.Favorite.Seed;
using Lyo.Seed;

namespace Lyo.Favorite.Seed.Tests;

public sealed class FavoriteEfSeedContributorTests
{
    [Fact]
    public async Task FavoriteEfSeedContributor_Count_PersistsFavorites()
    {
        var transport = await SeedAsync(new FavoriteEfSeedContributor(), 3);
        var rows = transport.Items.OfType<FavoriteEntity>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(rows.Count, rows.Select(row => row.ActorEntityId).Distinct().Count());
        Assert.Equal(rows.Count, rows.Select(row => row.SubjectEntityId).Distinct().Count());
        Assert.All(rows, row => {
            Assert.NotEqual(Guid.Empty, row.Id);
            Assert.Equal(FavoriteEfSeedContributor.TenantId, row.TenantId);
            Assert.Equal(FavoriteEfSeedContributor.SubjectType, row.SubjectEntityType);
            Assert.False(string.IsNullOrWhiteSpace(row.SubjectEntityId));
            Assert.Equal(EntityRefVisibility.Public, row.Visibility);
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
