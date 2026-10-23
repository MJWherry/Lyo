using Lyo.Common.Core.Identifiers;
using Lyo.Health;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.HomeInventory.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class HomeInventoryPostgresStoreTests(HomeInventoryPostgresFixture fixture)
{
    [Fact]
    public async Task SaveItem_ThenGetById_RoundTripsLocationId()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var location = new HomeLocationRecord { Name = "Office", Code = Unique("OFF") };
        await store.SaveLocationAsync(location, null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new() { Id = id, Name = "Desk lamp", LocationId = room.Id }, null, ct);
        var loaded = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(loaded);
        Assert.Equal("Desk lamp", loaded.Name);
        Assert.Equal(room.Id, loaded.LocationId);
    }

    [Fact]
    public async Task SaveItem_RoundTripsProductKeys_IncludingDuplicateKinds()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(
            new() {
                Id = id,
                Name = "AP",
                ProductKeys = [
                    new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:01", Note = "AP radio" },
                    new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:02", Note = "client NIC" }
                ]
            }, null, ct);

        var loaded = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.ProductKeys.Count);
        Assert.Equal(HomeProductKeys.WifiMac, loaded.ProductKeys[0].Kind);
        Assert.Equal("AA:BB:CC:DD:EE:01", loaded.ProductKeys[0].Value);
        Assert.Equal("AP radio", loaded.ProductKeys[0].Note);
        Assert.Equal("AA:BB:CC:DD:EE:02", loaded.ProductKeys[1].Value);
        Assert.Equal("client NIC", loaded.ProductKeys[1].Note);
    }

    [Fact]
    public async Task AdjustStock_ThenListMovements_RecordsReceipt()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var location = new HomeLocationRecord { Name = "Pantry", Code = Unique("PAN") };
        await store.SaveLocationAsync(location, null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new() { Id = id, Name = "Rice" }, null, ct);
        var saved = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(saved);
        await store.AdjustStockAsync(saved.Id, room.Id, 4, HomeItemMovementType.Receipt, "po-1", null, null, ct);
        var stock = await store.GetStockAsync(saved.Id, room.Id, null, ct);
        Assert.NotNull(stock);
        Assert.Equal(4m, stock.QuantityOnHand);
        var movements = await store.ListMovementsForItemAsync(saved.Id, 10, null, ct);
        Assert.Single(movements);
        Assert.Equal(HomeItemMovementType.Receipt, movements[0].MovementType);
        Assert.Equal("po-1", movements[0].ReferenceNumber);
    }

    [Fact]
    public async Task SaveAttachment_ThenDeleteItem_RemovesAttachmentRow()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new() { Id = id, Name = "Camera" }, null, ct);
        var saved = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(saved);
        var fileId = LyoGuid.CreateV7();
        await store.SaveAttachmentAsync(
            new() {
                EntityType = HomeInventoryEntityTypes.Item,
                EntityId = saved.Id,
                FileId = fileId,
                Role = HomeAttachmentRole.Photo,
                Caption = "front",
                IsPrimary = true
            }, null, ct);

        var listed = await store.ListAttachmentsAsync(HomeInventoryEntityTypes.Item, saved.Id, null, ct);
        Assert.Single(listed);
        Assert.Equal(fileId, listed[0].FileId);
        await store.DeleteItemAsync(saved.Id, null, ct);
        Assert.Empty(await store.ListAttachmentsAsync(HomeInventoryEntityTypes.Item, saved.Id, null, ct));
        Assert.Null(await store.GetItemByIdAsync(saved.Id, null, ct));
    }

    [Fact]
    public async Task DeleteLocation_SetsItemLocationIdNull()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var location = new HomeLocationRecord { Name = "Garage", Code = Unique("GAR") };
        await store.SaveLocationAsync(location, null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new() { Id = id, Name = "Bike", LocationId = room.Id }, null, ct);
        var saved = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(saved);
        Assert.Equal(room.Id, saved.LocationId);
        await store.DeleteLocationAsync(room.Id, null, ct);
        var after = await store.GetItemByIdAsync(id, null, ct);
        Assert.NotNull(after);
        Assert.Null(after.LocationId);
    }

    [Fact]
    public async Task HealthCheck_Succeeds()
    {
        var ct = TestCt;
        var health = (IHealth)fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var result = await health.CheckHealthAsync(ct);
        Assert.True(result.IsHealthy);
        Assert.Equal("home-inventory-postgres", health.HealthCheckName);
    }

    private static string Unique(string prefix) => prefix + "-" + LyoGuid.CreateV7().ToString("N")[..8];
}