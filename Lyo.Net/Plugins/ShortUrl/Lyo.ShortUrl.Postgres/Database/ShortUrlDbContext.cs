using Microsoft.EntityFrameworkCore;

namespace Lyo.ShortUrl.Postgres.Database;

public class ShortUrlDbContext : DbContext
{
    public DbSet<ShortUrlEntity> ShortUrls { get; set; } = null!;

    public DbSet<UrlClickEntity> UrlClicks { get; set; } = null!;

    public ShortUrlDbContext(DbContextOptions<ShortUrlDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("url");
        modelBuilder.ApplyConfiguration(new ShortUrlEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UrlClickEntityConfiguration());
    }
}