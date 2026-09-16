using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Ledger;
using Lyo.Ledger.Postgres;
using Lyo.Ledger.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Ledger.Seed;

/// <summary>EF seed of a cash account, a revenue account, and balanced posted journals. Schema <c>ledger</c>.</summary>
public sealed class LedgerEfSeedContributor : SeedContributor, IEfSeedSource
{
    private const decimal Amount = 25m;

    /// <inheritdoc />
    public override string Name => "Ledger";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresLedgerOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new LedgerDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var cashId = LyoGuid.CreateV7();
        var revenueId = LyoGuid.CreateV7();
        graph.Entity(1, _ => new LedgerAccountEntity {
            Id = cashId,
            Code = "1000",
            Name = "Cash",
            AccountType = (int)LedgerAccountType.Asset,
            CreatedAt = now
        });
        graph.Entity(1, _ => new LedgerAccountEntity {
            Id = revenueId,
            Code = "4000",
            Name = "Revenue",
            AccountType = (int)LedgerAccountType.Revenue,
            CreatedAt = now
        });
        graph.Entity(options.Count, i => new LedgerJournalEntity {
            Id = LyoGuid.CreateV7(),
            Status = (int)LedgerJournalStatus.Posted,
            Memo = faker.Commerce.ProductName(),
            PostedAt = now,
            CreatedAt = now
        }).ForEach(_ => 2, (journal, line) => new LedgerJournalLineEntity {
            Id = LyoGuid.CreateV7(),
            JournalId = journal.Id,
            LineNumber = line + 1,
            AccountId = line == 0 ? cashId : revenueId,
            Debit = line == 0 ? Amount : 0,
            Credit = line == 0 ? 0 : Amount,
            Memo = line == 0 ? "Debit cash" : "Credit revenue"
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<LedgerDbContext> ef)
            throw new SeedException("Ledger OnClear requires EfSeedTransport<LedgerDbContext>.");

        var db = ef.Context;
        await db.JournalLines.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Journals.ExecuteUpdateAsync(s => s.SetProperty(j => j.VoidOfJournalId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Journals.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Accounts.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
