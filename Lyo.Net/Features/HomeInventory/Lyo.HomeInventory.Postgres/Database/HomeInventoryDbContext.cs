using Microsoft.EntityFrameworkCore;

namespace Lyo.HomeInventory.Postgres.Database;

public class HomeInventoryDbContext : DbContext
{
    public DbSet<HomeCategoryEntity> Categories { get; set; } = null!;

    public DbSet<HomeLocationEntity> Locations { get; set; } = null!;

    public DbSet<HomeItemEntity> Items { get; set; } = null!;

    public DbSet<HomeItemStockEntity> Stocks { get; set; } = null!;

    public DbSet<HomeItemMovementEntity> Movements { get; set; } = null!;

    public HomeInventoryDbContext(DbContextOptions<HomeInventoryDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("home_inventory");
        modelBuilder.ApplyConfiguration(new HomeCategoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new HomeLocationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new HomeItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new HomeItemStockEntityConfiguration());
        modelBuilder.ApplyConfiguration(new HomeItemMovementEntityConfiguration());
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
            switch (entry.State) {
                case EntityState.Added:
                    switch (entry.Entity) {
                        case HomeCategoryEntity c when c.CreatedTimestamp == default:
                            c.CreatedTimestamp = now;
                            break;
                        case HomeLocationEntity l when l.CreatedTimestamp == default:
                            l.CreatedTimestamp = now;
                            break;
                        case HomeItemEntity i when i.CreatedTimestamp == default:
                            i.CreatedTimestamp = now;
                            break;
                        case HomeItemMovementEntity m when m.CreatedTimestamp == default:
                            m.CreatedTimestamp = now;
                            break;
                        case HomeItemStockEntity s:
                            s.UpdatedTimestamp = now;
                            break;
                    }

                    break;
                case EntityState.Modified:
                    switch (entry.Entity) {
                        case HomeCategoryEntity c:
                            c.UpdatedTimestamp = now;
                            break;
                        case HomeLocationEntity l:
                            l.UpdatedTimestamp = now;
                            break;
                        case HomeItemEntity i:
                            i.UpdatedTimestamp = now;
                            break;
                        case HomeItemStockEntity s:
                            s.UpdatedTimestamp = now;
                            break;
                    }

                    break;
            }
        }
    }
}