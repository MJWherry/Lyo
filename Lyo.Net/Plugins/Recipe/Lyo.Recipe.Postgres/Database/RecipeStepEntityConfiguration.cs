using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeStepEntityConfiguration : IEntityTypeConfiguration<RecipeStepEntity>
{
    public void Configure(EntityTypeBuilder<RecipeStepEntity> builder)
    {
        builder.ToTable("step");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.RecipeId).HasColumnName("recipe_id").HasColumnType("uuid");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.Instruction).HasMaxLength(8192).IsRequired().HasColumnName("instruction");
        builder.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
        builder.Property(e => e.GroupLabel).HasMaxLength(200).HasColumnName("group_label");
        builder.HasOne(e => e.Recipe).WithMany(e => e.Steps).HasForeignKey(e => e.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.RecipeId, e.SortOrder }).HasDatabaseName("ix_recipe_step_recipe_sort");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_step_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
