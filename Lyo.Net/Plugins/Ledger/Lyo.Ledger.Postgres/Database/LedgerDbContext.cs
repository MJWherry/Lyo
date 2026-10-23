using Microsoft.EntityFrameworkCore;

namespace Lyo.Ledger.Postgres.Database;

/// <summary>EF Core context for the <c>ledger</c> schema.</summary>
public class LedgerDbContext : DbContext
{
    /// <summary>Chart of accounts.</summary>
    public DbSet<LedgerAccountEntity> Accounts { get; set; } = null!;

    /// <summary>Journal headers.</summary>
    public DbSet<LedgerJournalEntity> Journals { get; set; } = null!;

    /// <summary>Journal lines.</summary>
    public DbSet<LedgerJournalLineEntity> JournalLines { get; set; } = null!;

    /// <summary>Constructs the context.</summary>
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresLedgerOptions.Schema);
        modelBuilder.ApplyConfiguration(new LedgerAccountEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerJournalEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerJournalLineEntityConfiguration());
    }
}