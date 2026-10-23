using Microsoft.EntityFrameworkCore;

namespace Lyo.Diagram.Postgres.Database;

/// <summary>EF Core context for diagram documents in PostgreSQL.</summary>
public sealed class DiagramDbContext : DbContext
{
    /// <summary>Stored diagrams.</summary>
    public DbSet<DiagramEntity> Diagrams { get; set; } = null!;

    /// <summary>Mints a context with the supplied options.</summary>
    public DiagramDbContext(DbContextOptions<DiagramDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresDiagramOptions.Schema);
        modelBuilder.ApplyConfiguration(new DiagramEntityConfiguration());
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
        foreach (var entry in ChangeTracker.Entries<DiagramEntity>()) {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}