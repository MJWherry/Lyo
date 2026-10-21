using Lyo.Geolocation.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Geolocation.Seed.Tests;

public sealed class GeolocationEfSeedContributorTests
{
    [Fact]
    public async Task GeolocationEfSeedContributor_Count_PersistsAddresses()
    {
        var transport = await SeedAsync(new GeolocationEfSeedContributor(), 2);
        var addresses = transport.Items.OfType<AddressEntity>().ToList();
        Assert.Equal(2, addresses.Count);
        Assert.All(
            addresses, address => {
                Assert.False(string.IsNullOrWhiteSpace(address.StreetAddress));
                Assert.Equal("US", address.CountryCode);
                Assert.False(string.IsNullOrWhiteSpace(address.FullAddress));
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