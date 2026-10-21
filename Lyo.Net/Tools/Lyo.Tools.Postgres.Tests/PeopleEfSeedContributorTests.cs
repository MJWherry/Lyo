using Lyo.People.Postgres.Database;
using Lyo.Seed;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Tools.Postgres.Tests;

public sealed class PeopleEfSeedContributorTests
{
    [Fact]
    public async Task PeopleEfSeedContributor_Count_PersistsIdentificationPerPerson()
    {
        var transport = await SeedAsync(new PeopleEfSeedContributor(NullLogger<PeopleEfSeedContributor>.Instance), 3);
        var people = transport.Items.OfType<PersonEntity>().ToList();
        var identifications = transport.Items.OfType<IdentificationEntity>().ToList();
        Assert.Equal(3, people.Count);
        Assert.Equal(3, identifications.Count);
        Assert.All(people, person => Assert.Contains(identifications, row => row.PersonId == person.Id && !string.IsNullOrWhiteSpace(row.Number)));
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