using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class UnitEntityConfiguration : IEntityTypeConfiguration<UnitEntity>
{
    public void Configure(EntityTypeBuilder<UnitEntity> builder)
    {
        builder.ToTable("unit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Name).HasMaxLength(80).IsRequired().HasColumnName("name");
        builder.Property(e => e.Abbreviation).HasMaxLength(40).IsRequired().HasColumnName("abbreviation");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_unit_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => new { e.TenantId, e.Abbreviation }).IsUnique().HasDatabaseName("ux_recipe_unit_abbr_tenant");
        builder.HasIndex(e => e.Abbreviation).IsUnique().HasDatabaseName("ux_recipe_unit_abbr").HasFilter("\"tenant_id\" IS NULL");
    }
}
