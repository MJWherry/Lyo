using Lyo.HomeInventory.Postgres.Database;
using Lyo.HomeInventory.Seed;
using Lyo.Seed;

namespace Lyo.HomeInventory.Seed.Tests;

public sealed class HomeInventoryEfSeedContributorTests
{
    [Fact]
    public async Task HomeInventoryEfSeedContributor_Count_PersistsItemsWithStockAndMovement()
    {
        var transport = await SeedAsync(new HomeInventoryEfSeedContributor(), 3);
        var items = transport.Items.OfType<HomeItemEntity>().ToList();
        var stock = transport.Items.OfType<HomeItemStockEntity>().ToList();
        var movements = transport.Items.OfType<HomeItemMovementEntity>().ToList();
        var attachments = transport.Items.OfType<HomeAttachmentEntity>().ToList();
        var categories = transport.Items.OfType<HomeCategoryEntity>().ToList();
        var locations = transport.Items.OfType<HomeLocationEntity>().ToList();
        Assert.True(categories.Count > 1);
        Assert.DoesNotContain(categories, row => row.Name == "Seed");
        Assert.True(locations.Count > 1);
        Assert.All(items, item => Assert.Contains(categories, row => row.Id == item.CategoryId));
        Assert.Equal(3, items.Count);
        Assert.Equal(3, stock.Count);
        Assert.Equal(3, movements.Count);
        Assert.Equal(3, attachments.Count);
        Assert.All(items, item => {
            Assert.Contains(stock, row => row.ItemId == item.Id);
            Assert.Contains(movements, row => row.ItemId == item.Id);
            Assert.Contains(attachments, row => row.EntityId == item.Id);
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
