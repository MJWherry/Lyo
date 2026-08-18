using Microsoft.EntityFrameworkCore;

namespace Lyo.Validation.Postgres.Database;

/// <summary>Entity Framework Core context for validation schemas in PostgreSQL.</summary>
public sealed class ValidationDbContext : DbContext
{
    /// <summary>Validation schema documents.</summary>
    public DbSet<ValidationSchemaEntity> Schemas { get; set; } = null!;

    /// <summary>Creates a context with the supplied options.</summary>
    public ValidationDbContext(DbContextOptions<ValidationDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresValidationOptions.Schema);
        modelBuilder.ApplyConfiguration(new ValidationSchemaEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ValidationSchemaEntity>()) {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}
