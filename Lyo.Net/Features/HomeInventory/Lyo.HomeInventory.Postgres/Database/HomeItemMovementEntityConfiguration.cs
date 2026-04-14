using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemMovementEntityConfiguration : IEntityTypeConfiguration<HomeItemMovementEntity>
{
    public void Configure(EntityTypeBuilder<HomeItemMovementEntity> builder)
    {
        builder.ToTable("movement");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ItemId).HasColumnName("item_id").HasColumnType("uuid");
        builder.Property(e => e.MovementType).HasColumnName("movement_type");
        builder.Property(e => e.Quantity).HasColumnType("decimal(18,4)").HasColumnName("quantity");
        builder.Property(e => e.FromLocationId).HasColumnName("from_location_id").HasColumnType("uuid");
        builder.Property(e => e.ToLocationId).HasColumnName("to_location_id").HasColumnType("uuid");
        builder.Property(e => e.ReferenceNumber).HasMaxLength(200).HasColumnName("reference_number");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedByEntityType).HasMaxLength(200).HasColumnName("created_by_entity_type");
        builder.Property(e => e.CreatedByEntityId).HasMaxLength(200).HasColumnName("created_by_entity_id");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasOne(e => e.Item).WithMany(e => e.Movements).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.FromLocation).WithMany(e => e.MovementsFrom).HasForeignKey(e => e.FromLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ToLocation).WithMany(e => e.MovementsTo).HasForeignKey(e => e.ToLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.ItemId).HasDatabaseName("ix_home_inv_movement_item");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_home_inv_movement_created");
    }
}