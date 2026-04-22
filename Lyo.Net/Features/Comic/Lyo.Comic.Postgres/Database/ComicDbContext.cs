using Microsoft.EntityFrameworkCore;

namespace Lyo.Comic.Postgres.Database;

public class ComicDbContext : DbContext
{
    public DbSet<SeriesEntity> Series { get; set; } = null!;

    public DbSet<AlternateTitleEntity> AlternateTitles { get; set; } = null!;

    public DbSet<VolumeEntity> Volumes { get; set; } = null!;

    public DbSet<ChapterEntity> Chapters { get; set; } = null!;

    public ComicDbContext(DbContextOptions<ComicDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comic");
        modelBuilder.ApplyConfiguration(new SeriesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlternateTitleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VolumeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ChapterEntityConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is SeriesEntity s && s.CreatedTimestamp == default)
                    s.CreatedTimestamp = now;
                else if (entry.Entity is VolumeEntity v && v.CreatedTimestamp == default)
                    v.CreatedTimestamp = now;
                else if (entry.Entity is ChapterEntity c && c.CreatedTimestamp == default)
                    c.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is SeriesEntity s)
                    s.UpdatedTimestamp = now;
                else if (entry.Entity is VolumeEntity v)
                    v.UpdatedTimestamp = now;
                else if (entry.Entity is ChapterEntity c)
                    c.UpdatedTimestamp = now;
            }
        }
    }
}
