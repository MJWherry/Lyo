using Lyo.ContactUs.Postgres.Database;
using Lyo.Seed;

namespace Lyo.ContactUs.Seed.Tests;

public sealed class ContactUsEfSeedContributorTests
{
    [Fact]
    public async Task ContactUsEfSeedContributor_Count_PersistsSubmissions()
    {
        var transport = await SeedAsync(new ContactUsEfSeedContributor(), 3);
        var rows = transport.Items.OfType<ContactSubmissionEntity>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.All(
            rows, row => {
                Assert.False(string.IsNullOrWhiteSpace(row.Name));
                Assert.False(string.IsNullOrWhiteSpace(row.Email));
                Assert.False(string.IsNullOrWhiteSpace(row.Subject));
                Assert.False(string.IsNullOrWhiteSpace(row.Message));
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