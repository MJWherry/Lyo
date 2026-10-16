using Microsoft.EntityFrameworkCore;

namespace Lyo.Audiobook.Postgres.Database;

public class AudiobookDbContext : DbContext
{
    public DbSet<AudiobookWorkEntity> Works { get; set; } = null!;

    public DbSet<AudiobookChapterEntity> Chapters { get; set; } = null!;

    public DbSet<AudiobookCreditEntity> Credits { get; set; } = null!;

    public DbSet<AudiobookIdentifierEntity> Identifiers { get; set; } = null!;

    public DbSet<AudiobookSeriesEntity> Series { get; set; } = null!;

    public DbSet<AudiobookGenreEntity> Genres { get; set; } = null!;

    public DbSet<AudiobookWorkGenreEntity> WorkGenres { get; set; } = null!;

    public AudiobookDbContext(DbContextOptions<AudiobookDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresAudiobookOptions.Schema);
        modelBuilder.ApplyConfiguration(new AudiobookWorkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookChapterEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookCreditEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookSeriesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudiobookWorkGenreEntityConfiguration());
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
                if (entry.Entity is AudiobookWorkEntity work && work.CreatedTimestamp == default)
                    work.CreatedTimestamp = now;
                else if (entry.Entity is AudiobookChapterEntity chapter && chapter.CreatedTimestamp == default)
                    chapter.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is AudiobookWorkEntity work)
                    work.UpdatedTimestamp = now;
                else if (entry.Entity is AudiobookChapterEntity chapter)
                    chapter.UpdatedTimestamp = now;
            }
        }
    }
}
