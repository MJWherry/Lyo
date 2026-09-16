using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Ledger.Postgres.Database;

/// <summary>EF migrations factory.</summary>
public class LedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    /// <inheritdoc />
    public LedgerDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<LedgerDbContext>("LEDGER_CONNECTION_STRING", PostgresLedgerOptions.Schema));
}
