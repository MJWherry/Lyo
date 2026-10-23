using Microsoft.EntityFrameworkCore;

namespace Lyo.Algorithms.Postgres.Database;

/// <summary>EF Core context for schema <c>algorithms</c>.</summary>
public sealed class AlgorithmDbContext : DbContext
{
    /// <summary>Catalog definitions.</summary>
    public DbSet<AlgorithmDefinitionEntity> Definitions => Set<AlgorithmDefinitionEntity>();

    /// <summary>Append-only revisions.</summary>
    public DbSet<AlgorithmRevisionEntity> Revisions => Set<AlgorithmRevisionEntity>();

    /// <summary>Labels pointing at revisions.</summary>
    public DbSet<AlgorithmLabelEntity> Labels => Set<AlgorithmLabelEntity>();

    /// <summary>Creates a context.</summary>
    public AlgorithmDbContext(DbContextOptions<AlgorithmDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresAlgorithmOptions.Schema);
        modelBuilder.ApplyConfiguration(new AlgorithmDefinitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlgorithmRevisionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlgorithmLabelEntityConfiguration());
    }
}