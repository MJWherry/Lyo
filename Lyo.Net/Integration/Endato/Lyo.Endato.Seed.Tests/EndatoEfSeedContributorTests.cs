using Lyo.Endato.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Endato.Seed.Tests;

public sealed class EndatoEfSeedContributorTests
{
    [Fact]
    public async Task EndatoEfSeedContributor_Count_PersistsSearchAndEnrichmentGraphs()
    {
        var transport = await SeedAsync(new EndatoEfSeedContributor(), 2);
        var searches = transport.Items.OfType<EndatoPsQueryEntity>().ToList();
        var enrichments = transport.Items.OfType<EndatoCeQueryEntity>().ToList();
        Assert.Equal(2, searches.Count);
        Assert.Equal(2, enrichments.Count);
        Assert.All(
            searches, query => {
                var person = Assert.Single(transport.Items.OfType<EndatoPsPersonEntity>(), row => row.QueryId == query.Id);
                Assert.Contains(transport.Items.OfType<EndatoPsAddressEntity>(), row => row.EndatoPersonId == person.Id);
                Assert.Contains(transport.Items.OfType<EndatoPsEmailAddressEntity>(), row => row.EndatoPersonId == person.Id);
                Assert.Contains(transport.Items.OfType<EndatoPsPhoneNumberEntity>(), row => row.EndatoPersonId == person.Id);
            });

        Assert.All(
            enrichments, query => {
                Assert.NotNull(query.EndatoCePersonId);
                var personId = query.EndatoCePersonId!.Value;
                Assert.Contains(transport.Items.OfType<EndatoCePersonEntity>(), row => row.Id == personId);
                Assert.Contains(transport.Items.OfType<EndatoCeAddressEntity>(), row => row.EndatoCePersonId == personId);
                Assert.Contains(transport.Items.OfType<EndatoCeEmailAddressEntity>(), row => row.EndatoCePersonId == personId);
                Assert.Contains(transport.Items.OfType<EndatoCePhoneNumberEntity>(), row => row.EndatoCePersonId == personId);
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