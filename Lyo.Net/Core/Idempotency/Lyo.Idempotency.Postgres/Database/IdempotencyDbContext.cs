using Microsoft.EntityFrameworkCore;

namespace Lyo.Idempotency.Postgres.Database;

/// <summary>EF model for the idempotency schema. Writes go through <see cref="PostgresIdempotencyStore" /> so a caller transaction can take a savepoint.</summary>
public sealed class IdempotencyDbContext : DbContext
{
    /// <summary>Builds a context.</summary>
    public IdempotencyDbContext(DbContextOptions<IdempotencyDbContext> options)
        : base(options) { }

    /// <summary>Idempotency rows.</summary>
    public DbSet<IdempotencyEntryEntity> Entries => Set<IdempotencyEntryEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresIdempotencyOptions.Schema);
        modelBuilder.ApplyConfiguration(new IdempotencyEntryEntityConfiguration());
    }
}
