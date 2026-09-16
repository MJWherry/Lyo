using Microsoft.EntityFrameworkCore;

namespace Lyo.Book.Postgres.Database;

public class BookDbContext : DbContext
{
    public DbSet<BookTypeEntity> Types { get; set; } = null!;

    public DbSet<BookFormatEntity> Formats { get; set; } = null!;

    public DbSet<BookGenreEntity> Genres { get; set; } = null!;

    public DbSet<BookSeriesEntity> Series { get; set; } = null!;

    public DbSet<BookSeriesAlternateTitleEntity> SeriesAlternateTitles { get; set; } = null!;

    public DbSet<BookEntity> Books { get; set; } = null!;

    public DbSet<BookAlternateTitleEntity> AlternateTitles { get; set; } = null!;

    public DbSet<BookIdentifierEntity> Identifiers { get; set; } = null!;

    public DbSet<BookContributorEntity> Contributors { get; set; } = null!;

    public DbSet<BookBookGenreEntity> BookGenres { get; set; } = null!;

    public DbSet<BookChapterEntity> Chapters { get; set; } = null!;

    public DbSet<BookPageEntity> Pages { get; set; } = null!;

    public BookDbContext(DbContextOptions<BookDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresBookOptions.Schema);
        modelBuilder.ApplyConfiguration(new BookTypeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookFormatEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookSeriesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookSeriesAlternateTitleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookAlternateTitleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookIdentifierEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookContributorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookBookGenreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookChapterEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BookPageEntityConfiguration());
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
                if (entry.Entity is BookSeriesEntity s && s.CreatedTimestamp == default)
                    s.CreatedTimestamp = now;
                else if (entry.Entity is BookEntity b && b.CreatedTimestamp == default)
                    b.CreatedTimestamp = now;
                else if (entry.Entity is BookChapterEntity c && c.CreatedTimestamp == default)
                    c.CreatedTimestamp = now;
                else if (entry.Entity is BookPageEntity p && p.CreatedTimestamp == default)
                    p.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is BookSeriesEntity s)
                    s.UpdatedTimestamp = now;
                else if (entry.Entity is BookEntity b)
                    b.UpdatedTimestamp = now;
                else if (entry.Entity is BookChapterEntity c)
                    c.UpdatedTimestamp = now;
                else if (entry.Entity is BookPageEntity p)
                    p.UpdatedTimestamp = now;
            }
        }
    }
}
