using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemStockEntityConfiguration : IEntityTypeConfiguration<HomeItemStockEntity>
{
    public void Configure(EntityTypeBuilder<HomeItemStockEntity> builder)
    {
        builder.ToTable("stock");
        builder.HasKey(e => new { e.ItemId, e.LocationId });
        builder.Property(e => e.ItemId).HasColumnName("item_id").HasColumnType("uuid");
        builder.Property(e => e.LocationId).HasColumnName("location_id").HasColumnType("uuid");
        builder.Property(e => e.QuantityOnHand).HasColumnType("decimal(18,4)").HasColumnName("quantity_on_hand");
        builder.Property(e => e.QuantityReserved).HasColumnType("decimal(18,4)").HasColumnName("quantity_reserved");
        builder.Property(e => e.ReorderPoint).HasColumnType("decimal(18,4)").HasColumnName("reorder_point");
        builder.Property(e => e.UpdatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Item).WithMany(e => e.StockRows).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Location).WithMany(e => e.StockRows).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.LocationId).HasDatabaseName("ix_home_inv_stock_location");
    }
}