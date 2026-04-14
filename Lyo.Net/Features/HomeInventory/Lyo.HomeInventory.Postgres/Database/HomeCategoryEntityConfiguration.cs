using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeCategoryEntityConfiguration : IEntityTypeConfiguration<HomeCategoryEntity>
{
    public void Configure(EntityTypeBuilder<HomeCategoryEntity> builder)
    {
        builder.ToTable("category");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id").HasColumnType("uuid");
        builder.Property(e => e.Name).HasMaxLength(300).IsRequired().HasColumnName("name");
        builder.Property(e => e.Slug).HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.ParentCategoryId).HasDatabaseName("ix_home_inv_category_parent");
        builder.HasIndex(e => new { e.Name, e.ParentCategoryId }).HasDatabaseName("ix_home_inv_category_name_parent");
    }
}