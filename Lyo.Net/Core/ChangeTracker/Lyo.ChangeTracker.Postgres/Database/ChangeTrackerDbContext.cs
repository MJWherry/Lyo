using Microsoft.EntityFrameworkCore;

namespace Lyo.ChangeTracker.Postgres.Database;

public class ChangeTrackerDbContext : DbContext
{
    public DbSet<ChangeEntryEntity> Changes { get; set; } = null!;

    public ChangeTrackerDbContext(DbContextOptions<ChangeTrackerDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresChangeTrackerOptions.Schema);
        modelBuilder.ApplyConfiguration(new ChangeEntryEntityConfiguration());
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
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is ChangeEntryEntity change && change.CreatedTimestamp == default)
                change.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified && entry.Entity is ChangeEntryEntity updated)
                updated.UpdatedTimestamp = now;
        }
    }
}