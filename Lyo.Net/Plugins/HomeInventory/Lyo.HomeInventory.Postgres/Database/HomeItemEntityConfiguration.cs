using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemEntityConfiguration : IEntityTypeConfiguration<HomeItemEntity>
{
    public void Configure(EntityTypeBuilder<HomeItemEntity> builder)
    {
        builder.ToTable("item");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.OwnerEntityType).HasMaxLength(200).HasColumnName("owner_entity_type");
        builder.Property(e => e.OwnerEntityId).HasMaxLength(200).HasColumnName("owner_entity_id");
        builder.Property(e => e.CategoryId).HasColumnName("category_id").HasColumnType("uuid");
        builder.Property(e => e.ParentItemId).HasColumnName("parent_item_id").HasColumnType("uuid");
        builder.Property(e => e.LocationId).HasColumnName("location_id").HasColumnType("uuid");
        builder.Property(e => e.Name).HasMaxLength(500).IsRequired().HasColumnName("name");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Condition).HasColumnName("condition");
        builder.Property(e => e.ProductKeysJson).HasColumnType("jsonb").IsRequired().HasColumnName("product_keys").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Category).WithMany(e => e.Items).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Location).WithMany(e => e.AssignedItems).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.ParentItem).WithMany(e => e.ChildItems).HasForeignKey(e => e.ParentItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.OwnerEntityType, e.OwnerEntityId }).HasDatabaseName("ix_home_inv_item_owner");
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("ix_home_inv_item_category");
        builder.HasIndex(e => e.LocationId).HasDatabaseName("ix_home_inv_item_location");
        builder.HasIndex(e => e.ParentItemId).HasDatabaseName("ix_home_inv_item_parent");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_home_inv_item_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
