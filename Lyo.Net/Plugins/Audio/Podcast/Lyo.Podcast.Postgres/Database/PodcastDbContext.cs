using Microsoft.EntityFrameworkCore;

namespace Lyo.Podcast.Postgres.Database;

public class PodcastDbContext : DbContext
{
    public DbSet<PodcastShowEntity> Shows { get; set; } = null!;

    public DbSet<PodcastSeasonEntity> Seasons { get; set; } = null!;

    public DbSet<PodcastEpisodeEntity> Episodes { get; set; } = null!;

    public DbSet<PodcastCreditEntity> Credits { get; set; } = null!;

    public DbSet<PodcastShowIdentifierEntity> ShowIdentifiers { get; set; } = null!;

    public DbSet<PodcastEpisodeIdentifierEntity> EpisodeIdentifiers { get; set; } = null!;

    public DbSet<PodcastGenreEntity> Genres { get; set; } = null!;

    public DbSet<PodcastShowGenreEntity> ShowGenres { get; set; } = null!;

    public PodcastDbContext(DbContextOptions<PodcastDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresPodcastOptions.Schema);
        modelBuilder.ApplyConfiguration(new PodcastShowEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastSeasonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastEpisodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastCreditEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastShowIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastEpisodeIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PodcastShowGenreEntityConfiguration());
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
                if (entry.Entity is PodcastShowEntity show && show.CreatedTimestamp == default)
                    show.CreatedTimestamp = now;
                else if (entry.Entity is PodcastEpisodeEntity episode && episode.CreatedTimestamp == default)
                    episode.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is PodcastShowEntity show)
                    show.UpdatedTimestamp = now;
                else if (entry.Entity is PodcastEpisodeEntity episode)
                    episode.UpdatedTimestamp = now;
            }
        }
    }
}