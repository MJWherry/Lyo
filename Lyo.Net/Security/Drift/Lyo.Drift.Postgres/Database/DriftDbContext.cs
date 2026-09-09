using Microsoft.EntityFrameworkCore;

namespace Lyo.Drift.Postgres.Database;

/// <summary>EF Core context for the <c>drift</c> schema.</summary>
public sealed class DriftDbContext : DbContext
{
    public DbSet<DriftInstance> Instances { get; set; } = null!;

    public DbSet<DriftStructureSnapshot> Snapshots { get; set; } = null!;

    public DbSet<DriftDiffSnapshot> Diffs { get; set; } = null!;

    public DbSet<DriftChangeEvent> Changes { get; set; } = null!;

    public DriftDbContext(DbContextOptions<DriftDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresDriftOptions.Schema);
        modelBuilder.ApplyConfiguration(new DriftInstanceConfiguration());
        modelBuilder.ApplyConfiguration(new DriftStructureSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new DriftDiffSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new DriftChangeEventConfiguration());
    }
}
