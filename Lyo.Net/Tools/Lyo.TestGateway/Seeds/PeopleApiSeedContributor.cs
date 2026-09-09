using Bogus;
using Lyo.People.Models;
using Lyo.Seed;
using Lyo.TestGateway.Models;

namespace Lyo.TestGateway.Seeds;

/// <summary>Bulk-inserts Person rows into TestApi via <c>POST Person/Bulk</c>. Nested contacts are dropped by TestApi mapping.</summary>
public sealed class PeopleApiSeedContributor : SeedContributor
{
    /// <inheritdoc />
    public override string Name => "People API";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Api;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(
            options.Count, _ => new PersonReq {
                FirstName = faker.Name.FirstName(),
                LastName = faker.Name.LastName(),
                MiddleName = faker.Random.Bool(0.4f) ? faker.Name.FirstName() : null,
                Source = PeopleSourceTypes.Seed
            });
    }
}
