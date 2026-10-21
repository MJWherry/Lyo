using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Geolocation.Postgres;
using Lyo.Geolocation.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Geolocation.Seed;

/// <summary>EF seed of street addresses. Schema <c>geolocation</c>.</summary>
public sealed class GeolocationEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Geolocation";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresGeolocationOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<GeolocationDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new GeolocationDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        graph.Entity(
            options.Count, i => {
                var number = (100 + i).ToString();
                var street = Fit(faker.Address.StreetName(), 40);
                var city = Fit(faker.Address.City(), 40);
                var state = faker.Address.StateAbbr();
                var zip = faker.Address.ZipCode("#####");
                var line = $"{number} {street} St";
                return new AddressEntity {
                    Id = LyoGuid.CreateV7(),
                    HouseNumber = number,
                    StreetName = street,
                    StreetType = "St",
                    StreetAddress = line,
                    City = city,
                    State = state,
                    Zipcode = zip,
                    CountryCode = "US",
                    FullAddress = $"{line}, {city}, {state} {zip}",
                    CreatedTimestamp = now
                };
            });
    }

    private static string Fit(string value, int max) => value.Length <= max ? value : value[..max];

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<GeolocationDbContext> ef)
            throw new SeedException("Geolocation OnClear requires EfSeedTransport<GeolocationDbContext>.");

        await ef.Context.Addresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}