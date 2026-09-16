using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class IngredientEntityConfiguration : IEntityTypeConfiguration<IngredientEntity>
{
    public void Configure(EntityTypeBuilder<IngredientEntity> builder)
    {
        builder.ToTable("ingredient");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Name).HasMaxLength(300).IsRequired().HasColumnName("name");
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired().HasColumnName("slug");
        builder.Property(e => e.DefaultUnit).HasMaxLength(80).HasColumnName("default_unit");
        builder.Property(e => e.Notes).HasMaxLength(8192).HasColumnName("notes");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_ingredient_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasDatabaseName("ux_recipe_ingredient_slug_tenant");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ux_recipe_ingredient_slug").HasFilter("\"tenant_id\" IS NULL");
    }
}
