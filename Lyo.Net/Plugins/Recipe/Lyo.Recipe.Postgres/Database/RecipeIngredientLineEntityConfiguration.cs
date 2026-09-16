using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeIngredientLineEntityConfiguration : IEntityTypeConfiguration<RecipeIngredientLineEntity>
{
    public void Configure(EntityTypeBuilder<RecipeIngredientLineEntity> builder)
    {
        builder.ToTable("ingredient_line");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.RecipeId).HasColumnName("recipe_id").HasColumnType("uuid");
        builder.Property(e => e.IngredientId).HasColumnName("ingredient_id").HasColumnType("uuid");
        builder.Property(e => e.IngredientText).HasMaxLength(500).HasColumnName("ingredient_text");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric");
        builder.Property(e => e.Unit).HasMaxLength(80).HasColumnName("unit");
        builder.Property(e => e.Preparation).HasMaxLength(300).HasColumnName("preparation");
        builder.Property(e => e.IsOptional).HasColumnName("is_optional");
        builder.Property(e => e.GroupLabel).HasMaxLength(200).HasColumnName("group_label");
        builder.HasOne(e => e.Recipe).WithMany(e => e.IngredientLines).HasForeignKey(e => e.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Ingredient).WithMany(e => e.Lines).HasForeignKey(e => e.IngredientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.RecipeId, e.SortOrder }).HasDatabaseName("ix_recipe_line_recipe_sort");
        builder.HasIndex(e => e.IngredientId).HasDatabaseName("ix_recipe_line_ingredient");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_line_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
