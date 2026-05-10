using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Rating.Postgres.Database;

public class RatingDbContext : EntityRefModuleDbContext
{
    public DbSet<RatingEntity> Ratings { get; set; } = null!;

    public DbSet<RatingReactionEntity> RatingReactions { get; set; } = null!;

    public RatingDbContext(DbContextOptions<RatingDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rating");
        modelBuilder.ApplyConfiguration(new RatingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RatingReactionEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampRatingTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampRatingTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampRatingTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<RatingEntity>()) {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}
