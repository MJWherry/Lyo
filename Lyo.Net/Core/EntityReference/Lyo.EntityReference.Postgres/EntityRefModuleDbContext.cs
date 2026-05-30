using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.EntityReference.Postgres;

/// <summary>Base <see cref="DbContext" /> that stamps <see cref="EntityRelationEntityBase.CreatedAt" /> on insert when left at default.</summary>
public abstract class EntityRefModuleDbContext : DbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">Options for this context instance.</param>
    protected EntityRefModuleDbContext(DbContextOptions options)
        : base(options) { }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampCreatedAtUtc();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampCreatedAtUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampCreatedAtUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampCreatedAtUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Sets <see cref="EntityRelationEntityBase.CreatedAt" /> to UTC now for added <see cref="EntityRelationEntityBase" /> entries when still default.</summary>
    private void StampCreatedAtUtc()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityRelationEntityBase>()) {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                entry.Entity.CreatedAt = utcNow;
        }
    }
}