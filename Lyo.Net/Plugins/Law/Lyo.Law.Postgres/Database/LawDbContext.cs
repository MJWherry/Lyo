using Microsoft.EntityFrameworkCore;

namespace Lyo.Law.Postgres.Database;

/// <summary>EF Core context for the state statute schema.</summary>
public class LawDbContext : DbContext
{
    /// <summary>States.</summary>
    public DbSet<LawJurisdictionEntity> Jurisdictions { get; set; } = null!;

    /// <summary>Named compilations.</summary>
    public DbSet<LawCodeEntity> Codes { get; set; } = null!;

    /// <summary>Outline and text nodes.</summary>
    public DbSet<LawNodeEntity> Nodes { get; set; } = null!;

    /// <summary>Official notes.</summary>
    public DbSet<LawAnnotationEntity> Annotations { get; set; } = null!;

    /// <summary>Prior node text.</summary>
    public DbSet<LawRevisionEntity> Revisions { get; set; } = null!;

    /// <summary>Creates a context with the given options.</summary>
    public LawDbContext(DbContextOptions<LawDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresLawOptions.Schema);
        modelBuilder.ApplyConfiguration(new LawJurisdictionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LawCodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LawNodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LawAnnotationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LawRevisionEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            switch (entry.State) {
                case EntityState.Added:
                    StampAdded(entry.Entity, now);
                    break;
                case EntityState.Modified:
                    StampModified(entry.Entity, now);
                    break;
            }
        }
    }

    private static void StampAdded(object entity, DateTime now)
    {
        switch (entity) {
            case LawJurisdictionEntity row when row.CreatedTimestamp == default:
                row.CreatedTimestamp = now;
                break;
            case LawCodeEntity row when row.CreatedTimestamp == default:
                row.CreatedTimestamp = now;
                break;
            case LawNodeEntity row when row.CreatedTimestamp == default:
                row.CreatedTimestamp = now;
                break;
            case LawAnnotationEntity row when row.CreatedTimestamp == default:
                row.CreatedTimestamp = now;
                break;
            case LawRevisionEntity row when row.CreatedTimestamp == default:
                row.CreatedTimestamp = now;
                break;
        }
    }

    private static void StampModified(object entity, DateTime now)
    {
        switch (entity) {
            case LawJurisdictionEntity row:
                row.UpdatedTimestamp = now;
                break;
            case LawCodeEntity row:
                row.UpdatedTimestamp = now;
                break;
            case LawNodeEntity row:
                row.UpdatedTimestamp = now;
                break;
        }
    }
}
