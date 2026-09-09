using Bogus;
using Lyo.People.Models;
using Lyo.Seed;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>JSON payload for TestApi <c>POST Person/Bulk</c> (scalars only; nested contacts are dropped by TestApi mapping).</summary>
public sealed class PersonSeedRequest
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public string Source { get; set; } = PeopleSourceTypes.Seed;
}

/// <summary>Bulk-inserts Person rows through Lyo.Api <c>POST Person/Bulk</c>.</summary>
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
            options.Count, _ => new PersonSeedRequest {
                FirstName = faker.Name.FirstName(),
                LastName = faker.Name.LastName(),
                MiddleName = faker.Random.Bool(0.4f) ? faker.Name.FirstName() : null,
                Source = PeopleSourceTypes.Seed
            });
    }
}
