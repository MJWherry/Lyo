using Microsoft.EntityFrameworkCore;

namespace Lyo.Tag.Postgres.Database;

public class TagDbContext : DbContext
{
    public DbSet<TagEntity> Tags { get; set; } = null!;

    public TagDbContext(DbContextOptions<TagDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tag");
        modelBuilder.ApplyConfiguration(new TagEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is TagEntity t && t.CreatedTimestamp == default)
                t.CreatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}