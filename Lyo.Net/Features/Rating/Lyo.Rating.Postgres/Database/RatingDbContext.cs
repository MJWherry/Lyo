using Microsoft.EntityFrameworkCore;

namespace Lyo.Rating.Postgres.Database;

public class RatingDbContext : DbContext
{
    public DbSet<RatingEntity> Ratings { get; set; } = null!;

    public DbSet<RatingReactionEntity> RatingReactions { get; set; } = null!;

    public RatingDbContext(DbContextOptions<RatingDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rating");
        modelBuilder.ApplyConfiguration(new RatingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RatingReactionEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is RatingEntity r && r.CreatedTimestamp == default)
                r.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified && entry.Entity is RatingEntity r2)
                r2.UpdatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}