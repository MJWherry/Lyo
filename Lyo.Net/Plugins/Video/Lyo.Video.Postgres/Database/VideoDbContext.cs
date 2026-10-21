using Microsoft.EntityFrameworkCore;

namespace Lyo.Video.Postgres.Database;

public class VideoDbContext : DbContext
{
    public DbSet<VideoTitleEntity> Titles { get; set; } = null!;

    public DbSet<VideoSeasonEntity> Seasons { get; set; } = null!;

    public DbSet<VideoEpisodeEntity> Episodes { get; set; } = null!;

    public DbSet<VideoCreditEntity> Credits { get; set; } = null!;

    public DbSet<VideoTitleIdentifierEntity> TitleIdentifiers { get; set; } = null!;

    public DbSet<VideoEpisodeIdentifierEntity> EpisodeIdentifiers { get; set; } = null!;

    public DbSet<VideoYoutubeLinkEntity> YoutubeLinks { get; set; } = null!;

    public DbSet<VideoGenreEntity> Genres { get; set; } = null!;

    public DbSet<VideoTitleGenreEntity> TitleGenres { get; set; } = null!;

    public DbSet<VideoAlternateTitleEntity> AlternateTitles { get; set; } = null!;

    public DbSet<VideoCollectionEntity> Collections { get; set; } = null!;

    public DbSet<VideoCutEntity> Cuts { get; set; } = null!;

    public VideoDbContext(DbContextOptions<VideoDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresVideoOptions.Schema);
        modelBuilder.ApplyConfiguration(new VideoTitleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoSeasonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoEpisodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoCreditEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoTitleIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoEpisodeIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoYoutubeLinkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoTitleGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoAlternateTitleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoCollectionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VideoCutEntityConfiguration());
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
                if (entry.Entity is VideoTitleEntity t && t.CreatedTimestamp == default)
                    t.CreatedTimestamp = now;
                else if (entry.Entity is VideoEpisodeEntity e && e.CreatedTimestamp == default)
                    e.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is VideoTitleEntity t)
                    t.UpdatedTimestamp = now;
                else if (entry.Entity is VideoEpisodeEntity e)
                    e.UpdatedTimestamp = now;
            }
        }
    }
}