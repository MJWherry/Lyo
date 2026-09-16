using Lyo.Ledger.Postgres.Database;
using Lyo.Ledger.Seed;
using Lyo.Seed;

namespace Lyo.Ledger.Seed.Tests;

public sealed class LedgerEfSeedContributorTests
{
    [Fact]
    public async Task LedgerEfSeedContributor_Journals_Balance()
    {
        var transport = await SeedAsync(new LedgerEfSeedContributor(), 3);
        var accounts = transport.Items.OfType<LedgerAccountEntity>().ToList();
        var journals = transport.Items.OfType<LedgerJournalEntity>().ToList();
        var lines = transport.Items.OfType<LedgerJournalLineEntity>().ToList();
        Assert.Equal(2, accounts.Count);
        Assert.Equal(3, journals.Count);
        Assert.All(journals, journal => {
            var journalLines = lines.Where(line => line.JournalId == journal.Id).ToList();
            Assert.Equal(2, journalLines.Count);
            Assert.Equal(journalLines.Sum(line => line.Debit), journalLines.Sum(line => line.Credit));
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
