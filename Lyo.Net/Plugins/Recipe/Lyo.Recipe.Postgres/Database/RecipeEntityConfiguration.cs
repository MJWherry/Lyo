using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeEntityConfiguration : IEntityTypeConfiguration<RecipeEntity>
{
    public void Configure(EntityTypeBuilder<RecipeEntity> builder)
    {
        builder.ToTable("recipe");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired().HasColumnName("title");
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired().HasColumnName("slug");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Servings).HasColumnName("servings");
        builder.Property(e => e.PrepMinutes).HasColumnName("prep_minutes");
        builder.Property(e => e.CookMinutes).HasColumnName("cook_minutes");
        builder.Property(e => e.Difficulty).HasColumnName("difficulty");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.SourceName).HasMaxLength(300).HasColumnName("source_name");
        builder.Property(e => e.SourceUrl).HasMaxLength(2000).HasColumnName("source_url");
        builder.Property(e => e.YieldLabel).HasMaxLength(200).HasColumnName("yield_label");
        builder.Property(e => e.CategoryId).HasColumnName("category_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Category).WithMany(e => e.Recipes).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("ix_recipe_recipe_category");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_recipe_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasDatabaseName("ux_recipe_recipe_slug_tenant");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ux_recipe_recipe_slug").HasFilter("\"tenant_id\" IS NULL");
    }
}
