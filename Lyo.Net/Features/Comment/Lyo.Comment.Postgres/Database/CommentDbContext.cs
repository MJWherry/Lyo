using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Comment.Postgres.Database;

public class CommentDbContext : EntityRefModuleDbContext
{
    public DbSet<CommentEntity> Comments { get; set; } = null!;

    public DbSet<CommentReactionEntity> CommentReactions { get; set; } = null!;

    public CommentDbContext(DbContextOptions<CommentDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comment");
        modelBuilder.ApplyConfiguration(new CommentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommentReactionEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampCommentTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampCommentTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampCommentTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<CommentEntity>()) {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}
