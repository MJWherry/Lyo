using Lyo.HomeInventory;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

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
        await store.SaveLocationAsync(location, tenantId: null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new HomeItemRecord { Id = id, Name = "Desk lamp", LocationId = room.Id }, tenantId: null, ct);
        var loaded = await store.GetItemByIdAsync(id, tenantId: null, ct);
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
            new HomeItemRecord {
                Id = id,
                Name = "AP",
                ProductKeys = [
                    new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:01", Note = "AP radio" },
                    new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:02", Note = "client NIC" }
                ]
            }, tenantId: null, ct);
        var loaded = await store.GetItemByIdAsync(id, tenantId: null, ct);
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
        await store.SaveLocationAsync(location, tenantId: null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new HomeItemRecord { Id = id, Name = "Rice" }, tenantId: null, ct);
        var saved = await store.GetItemByIdAsync(id, tenantId: null, ct);
        Assert.NotNull(saved);
        await store.AdjustStockAsync(saved.Id, room.Id, 4, HomeItemMovementType.Receipt, "po-1", createdBy: null, tenantId: null, ct);
        var stock = await store.GetStockAsync(saved.Id, room.Id, tenantId: null, ct);
        Assert.NotNull(stock);
        Assert.Equal(4m, stock.QuantityOnHand);
        var movements = await store.ListMovementsForItemAsync(saved.Id, 10, tenantId: null, ct);
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
        await store.SaveItemAsync(new HomeItemRecord { Id = id, Name = "Camera" }, tenantId: null, ct);
        var saved = await store.GetItemByIdAsync(id, tenantId: null, ct);
        Assert.NotNull(saved);
        var fileId = LyoGuid.CreateV7();
        await store.SaveAttachmentAsync(
            new HomeAttachmentRecord {
                EntityType = HomeInventoryEntityTypes.Item,
                EntityId = saved.Id,
                FileId = fileId,
                Role = HomeAttachmentRole.Photo,
                Caption = "front",
                IsPrimary = true
            }, tenantId: null, ct);
        var listed = await store.ListAttachmentsAsync(HomeInventoryEntityTypes.Item, saved.Id, tenantId: null, ct);
        Assert.Single(listed);
        Assert.Equal(fileId, listed[0].FileId);
        await store.DeleteItemAsync(saved.Id, tenantId: null, ct);
        Assert.Empty(await store.ListAttachmentsAsync(HomeInventoryEntityTypes.Item, saved.Id, tenantId: null, ct));
        Assert.Null(await store.GetItemByIdAsync(saved.Id, tenantId: null, ct));
    }

    [Fact]
    public async Task DeleteLocation_SetsItemLocationIdNull()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var location = new HomeLocationRecord { Name = "Garage", Code = Unique("GAR") };
        await store.SaveLocationAsync(location, tenantId: null, ct);
        var room = (await store.ListLocationsAsync(true, null, ct)).Single(l => l.Code == location.Code);
        var id = LyoGuid.CreateV7();
        await store.SaveItemAsync(new HomeItemRecord { Id = id, Name = "Bike", LocationId = room.Id }, tenantId: null, ct);
        var saved = await store.GetItemByIdAsync(id, tenantId: null, ct);
        Assert.NotNull(saved);
        Assert.Equal(room.Id, saved.LocationId);
        await store.DeleteLocationAsync(room.Id, tenantId: null, ct);
        var after = await store.GetItemByIdAsync(id, tenantId: null, ct);
        Assert.NotNull(after);
        Assert.Null(after.LocationId);
    }

    [Fact]
    public async Task HealthCheck_Succeeds()
    {
        var ct = TestCt;
        var health = (Lyo.Health.IHealth)fixture.ServiceProvider.GetRequiredService<IHomeInventoryStore>();
        var result = await health.CheckHealthAsync(ct);
        Assert.True(result.IsHealthy);
        Assert.Equal("home-inventory-postgres", health.HealthCheckName);
    }

    private static string Unique(string prefix) => prefix + "-" + LyoGuid.CreateV7().ToString("N")[..8];
}
