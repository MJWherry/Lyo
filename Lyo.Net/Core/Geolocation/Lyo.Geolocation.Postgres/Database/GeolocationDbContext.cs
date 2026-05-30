using Microsoft.EntityFrameworkCore;

namespace Lyo.Geolocation.Postgres.Database;

/// <summary>Entity Framework Core DbContext for geolocation data in PostgreSQL.</summary>
public class GeolocationDbContext : DbContext
{
    public DbSet<AddressEntity> Addresses { get; set; } = null!;

    public GeolocationDbContext(DbContextOptions<GeolocationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresGeolocationOptions.Schema);
        modelBuilder.ApplyConfiguration(new AddressEntityConfiguration());
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AddressEntity>()) {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}
