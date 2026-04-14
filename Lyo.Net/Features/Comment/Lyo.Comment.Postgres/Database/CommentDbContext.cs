using Microsoft.EntityFrameworkCore;

namespace Lyo.Comment.Postgres.Database;

public class CommentDbContext : DbContext
{
    public DbSet<CommentEntity> Comments { get; set; } = null!;

    public DbSet<CommentReactionEntity> CommentReactions { get; set; } = null!;

    public CommentDbContext(DbContextOptions<CommentDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comment");
        modelBuilder.ApplyConfiguration(new CommentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommentReactionEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is CommentEntity c && c.CreatedTimestamp == default)
                c.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified && entry.Entity is CommentEntity c2)
                c2.UpdatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}