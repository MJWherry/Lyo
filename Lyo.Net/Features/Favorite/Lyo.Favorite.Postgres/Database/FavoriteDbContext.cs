using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Favorite.Postgres.Database;

public class FavoriteDbContext : EntityRefModuleDbContext
{
    public DbSet<FavoriteEntity> Favorites { get; set; } = null!;

    public FavoriteDbContext(DbContextOptions<FavoriteDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("favorite");
        modelBuilder.ApplyConfiguration(new FavoriteEntityConfiguration());
    }
}
