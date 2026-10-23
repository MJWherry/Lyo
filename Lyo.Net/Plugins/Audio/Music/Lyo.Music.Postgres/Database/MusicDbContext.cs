using Microsoft.EntityFrameworkCore;

namespace Lyo.Music.Postgres.Database;

public class MusicDbContext : DbContext
{
    public DbSet<MusicReleaseGroupEntity> ReleaseGroups { get; set; } = null!;

    public DbSet<MusicReleaseEntity> Releases { get; set; } = null!;

    public DbSet<MusicTrackEntity> Tracks { get; set; } = null!;

    public DbSet<MusicCreditEntity> Credits { get; set; } = null!;

    public DbSet<MusicIdentifierEntity> Identifiers { get; set; } = null!;

    public DbSet<MusicGenreEntity> Genres { get; set; } = null!;

    public DbSet<MusicFormatEntity> Formats { get; set; } = null!;

    public DbSet<MusicReleaseGenreEntity> ReleaseGenres { get; set; } = null!;

    public DbSet<MusicRecordingEntity> Recordings { get; set; } = null!;

    public MusicDbContext(DbContextOptions<MusicDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresMusicOptions.Schema);
        modelBuilder.ApplyConfiguration(new MusicReleaseGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicReleaseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicTrackEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicCreditEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicFormatEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicReleaseGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MusicRecordingEntityConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is MusicReleaseGroupEntity g && g.CreatedTimestamp == default)
                    g.CreatedTimestamp = now;
                else if (entry.Entity is MusicReleaseEntity r && r.CreatedTimestamp == default)
                    r.CreatedTimestamp = now;
                else if (entry.Entity is MusicTrackEntity t && t.CreatedTimestamp == default)
                    t.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is MusicReleaseGroupEntity g)
                    g.UpdatedTimestamp = now;
                else if (entry.Entity is MusicReleaseEntity r)
                    r.UpdatedTimestamp = now;
                else if (entry.Entity is MusicTrackEntity t)
                    t.UpdatedTimestamp = now;
            }
        }
    }
}