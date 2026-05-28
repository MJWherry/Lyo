using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Common.Enums;
using Lyo.Geolocation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Geolocation.Postgres.Tests;

public class GeolocationPostgresExtensionsTests
{
    private readonly GeolocationPostgresFixture _fixture;

    public GeolocationPostgresExtensionsTests(GeolocationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void AddPostgresGeolocationStore_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresGeolocationStore(null!, _ => { }));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddGeolocationDbContextFactoryFromConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddGeolocationDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_CanConnectAndQuerySchema()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<GeolocationDbContext>>();
        await using var context = factory.CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<GeolocationDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Store_SaveAndGetAddress_RoundTrips()
    {
        var store = _fixture.ServiceProvider.GetRequiredService<IGeolocationStore>();
        var placeId = "test-place-id";
        var address = new Address {
            StreetAddress = "1600 Amphitheatre Parkway",
            City = "Mountain View",
            State = "CA",
            Zipcode = "94043",
            CountryCode = CountryCode.US,
            Sources = {
                new EntitySourceRecord(
                    EntityRef.ForKey("GoogleMapsPlace", placeId),
                    DateTime.UtcNow)
            }
        };

        await store.SaveAddressAsync(address, TestContext.Current.CancellationToken);
        var loaded = await store.GetAddressByIdAsync(address.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("Mountain View", loaded.City);
        Assert.Single(loaded.Sources);
        Assert.Equal(placeId, loaded.Sources.First().Source.EntityId);

        var bySource = await store.GetBySourceAsync(
            EntityRef.ForKey("GoogleMapsPlace", placeId),
            TestContext.Current.CancellationToken);
        Assert.NotNull(bySource);
        Assert.Equal(address.Id, bySource.Id);
    }
}
