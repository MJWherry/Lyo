using Microsoft.EntityFrameworkCore;

namespace Lyo.Favorite.Postgres.Database;

public class FavoriteDbContext : DbContext
{
    public DbSet<FavoriteEntity> Favorites { get; set; } = null!;

    public FavoriteDbContext(DbContextOptions<FavoriteDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("favorite");
        modelBuilder.ApplyConfiguration(new FavoriteEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is FavoriteEntity f && f.CreatedTimestamp == default)
                f.CreatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}