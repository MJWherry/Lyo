using Microsoft.EntityFrameworkCore;

namespace Lyo.Story.Sqlite.Database;

/// <summary>EF Core context for the local SQLite story database. Same tables as the Postgres store, without a schema.</summary>
public class StoryDbContext : DbContext
{
    public DbSet<StoryEntity> Stories { get; set; } = null!;

    public DbSet<StorySceneEntity> Scenes { get; set; } = null!;

    public DbSet<StoryNodeEntity> Nodes { get; set; } = null!;

    public DbSet<StoryVariableEntity> Variables { get; set; } = null!;

    public DbSet<StoryActionEntity> Actions { get; set; } = null!;

    public DbSet<StoryChoiceEntity> Choices { get; set; } = null!;

    public DbSet<StoryReactionEntity> Reactions { get; set; } = null!;

    public DbSet<StoryEffectEntity> Effects { get; set; } = null!;

    public DbSet<StoryPlaythroughEntity> Playthroughs { get; set; } = null!;

    public DbSet<StoryPlaythroughVariableEntity> PlaythroughVariables { get; set; } = null!;

    public DbSet<StoryPlaythroughEventEntity> PlaythroughEvents { get; set; } = null!;

    public StoryDbContext(DbContextOptions<StoryDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StorySceneEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryNodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryVariableEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryActionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryChoiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryReactionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryEffectEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryPlaythroughEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryPlaythroughVariableEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryPlaythroughEventEntityConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<StoryEntity>()) {
            if (entry.State == EntityState.Added && entry.Entity.CreatedTimestamp == default)
                entry.Entity.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}