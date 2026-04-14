using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeLocationEntityConfiguration : IEntityTypeConfiguration<HomeLocationEntity>
{
    public void Configure(EntityTypeBuilder<HomeLocationEntity> builder)
    {
        builder.ToTable("location");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentLocationId).HasColumnName("parent_location_id").HasColumnType("uuid");
        builder.Property(e => e.Name).HasMaxLength(300).IsRequired().HasColumnName("name");
        builder.Property(e => e.Code).HasMaxLength(100).HasColumnName("code");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.ParentLocationId).HasDatabaseName("ix_home_inv_location_parent");
        builder.HasIndex(e => e.Code).HasDatabaseName("ix_home_inv_location_code");
    }
}