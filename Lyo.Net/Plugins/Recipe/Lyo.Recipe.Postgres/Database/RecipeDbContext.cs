using Microsoft.EntityFrameworkCore;

namespace Lyo.Recipe.Postgres.Database;

/// <summary>EF Core context for the recipe schema.</summary>
public class RecipeDbContext : DbContext
{
    public DbSet<RecipeCategoryEntity> Categories { get; set; } = null!;

    public DbSet<IngredientEntity> Ingredients { get; set; } = null!;

    public DbSet<RecipeEntity> Recipes { get; set; } = null!;

    public DbSet<RecipeIngredientLineEntity> IngredientLines { get; set; } = null!;

    public DbSet<RecipeIngredientLineStepEntity> IngredientLineSteps { get; set; } = null!;

    public DbSet<RecipeStepEntity> Steps { get; set; } = null!;

    public DbSet<UnitEntity> Units { get; set; } = null!;

    public DbSet<RecipeAttachmentEntity> Attachments { get; set; } = null!;

    public RecipeDbContext(DbContextOptions<RecipeDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresRecipeOptions.Schema);
        modelBuilder.ApplyConfiguration(new RecipeCategoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new IngredientEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeIngredientLineEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeIngredientLineStepEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeStepEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UnitEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeAttachmentEntityConfiguration());
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
        foreach (var entry in ChangeTracker.Entries()) {
            switch (entry.State) {
                case EntityState.Added:
                    switch (entry.Entity) {
                        case RecipeCategoryEntity c when c.CreatedTimestamp == default:
                            c.CreatedTimestamp = now;
                            break;
                        case IngredientEntity i when i.CreatedTimestamp == default:
                            i.CreatedTimestamp = now;
                            break;
                        case RecipeEntity r when r.CreatedTimestamp == default:
                            r.CreatedTimestamp = now;
                            break;
                        case RecipeAttachmentEntity a when a.CreatedTimestamp == default:
                            a.CreatedTimestamp = now;
                            break;
                    }

                    break;
                case EntityState.Modified:
                    switch (entry.Entity) {
                        case RecipeCategoryEntity c:
                            c.UpdatedTimestamp = now;
                            break;
                        case IngredientEntity i:
                            i.UpdatedTimestamp = now;
                            break;
                        case RecipeEntity r:
                            r.UpdatedTimestamp = now;
                            break;
                        case RecipeAttachmentEntity a:
                            a.UpdatedTimestamp = now;
                            break;
                    }

                    break;
            }
        }
    }
}
