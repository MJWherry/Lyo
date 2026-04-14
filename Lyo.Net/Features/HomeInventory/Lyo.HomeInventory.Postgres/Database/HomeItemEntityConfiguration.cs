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
        builder.Property(e => e.OwnerEntityType).HasMaxLength(200).HasColumnName("owner_entity_type");
        builder.Property(e => e.OwnerEntityId).HasMaxLength(200).HasColumnName("owner_entity_id");
        builder.Property(e => e.CategoryId).HasColumnName("category_id").HasColumnType("uuid");
        builder.Property(e => e.ParentItemId).HasColumnName("parent_item_id").HasColumnType("uuid");
        builder.Property(e => e.Name).HasMaxLength(500).IsRequired().HasColumnName("name");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Condition).HasColumnName("condition");
        builder.Property(e => e.Sku).HasMaxLength(120).HasColumnName("sku");
        builder.Property(e => e.PurchaseOrderNumber).HasMaxLength(120).HasColumnName("purchase_order_number");
        builder.Property(e => e.SalesOrderNumber).HasMaxLength(120).HasColumnName("sales_order_number");
        builder.Property(e => e.Manufacturer).HasMaxLength(300).HasColumnName("manufacturer");
        builder.Property(e => e.ManufacturerPartNumber).HasMaxLength(200).HasColumnName("manufacturer_part_number");
        builder.Property(e => e.Seller).HasMaxLength(300).HasColumnName("seller");
        builder.Property(e => e.VendorSku).HasMaxLength(120).HasColumnName("vendor_sku");
        builder.Property(e => e.Upc).HasMaxLength(32).HasColumnName("upc");
        builder.Property(e => e.Ean).HasMaxLength(32).HasColumnName("ean");
        builder.Property(e => e.Isbn).HasMaxLength(32).HasColumnName("isbn");
        builder.Property(e => e.ModelNumber).HasMaxLength(200).HasColumnName("model_number");
        builder.Property(e => e.Color).HasMaxLength(120).HasColumnName("color");
        builder.Property(e => e.SerialNumber).HasMaxLength(200).HasColumnName("serial_number");
        builder.Property(e => e.Imei).HasMaxLength(64).HasColumnName("imei");
        builder.Property(e => e.EthernetMacAddress).HasMaxLength(32).HasColumnName("ethernet_mac_address");
        builder.Property(e => e.WifiMacAddress).HasMaxLength(32).HasColumnName("wifi_mac_address");
        builder.Property(e => e.BluetoothMacAddress).HasMaxLength(32).HasColumnName("bluetooth_mac_address");
        builder.Property(e => e.Msrp).HasColumnType("decimal(18,4)").HasColumnName("msrp");
        builder.Property(e => e.Cost).HasColumnType("decimal(18,4)").HasColumnName("cost");
        builder.Property(e => e.Currency).HasMaxLength(3).HasColumnName("currency");
        builder.Property(e => e.WeightGrams).HasColumnName("weight_grams");
        builder.Property(e => e.LengthMm).HasColumnName("length_mm");
        builder.Property(e => e.WidthMm).HasColumnName("width_mm");
        builder.Property(e => e.HeightMm).HasColumnName("height_mm");
        builder.Property(e => e.AcquiredDate).HasColumnType("date").HasColumnName("acquired_date");
        builder.Property(e => e.WarrantyExpires).HasColumnType("date").HasColumnName("warranty_expires");
        builder.Property(e => e.CountryOfOrigin).HasMaxLength(2).HasColumnName("country_of_origin");
        builder.Property(e => e.LotNumber).HasMaxLength(120).HasColumnName("lot_number");
        builder.Property(e => e.BatchNumber).HasMaxLength(120).HasColumnName("batch_number");
        builder.Property(e => e.CustomAttributesJson).HasColumnType("jsonb").HasColumnName("custom_attributes_json");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Category).WithMany(e => e.Items).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ParentItem).WithMany(e => e.ChildItems).HasForeignKey(e => e.ParentItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.OwnerEntityType, e.OwnerEntityId }).HasDatabaseName("ix_home_inv_item_owner");
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("ix_home_inv_item_category");
        builder.HasIndex(e => e.ParentItemId).HasDatabaseName("ix_home_inv_item_parent");
        builder.HasIndex(e => e.SerialNumber).HasDatabaseName("ix_home_inv_item_serial");
        builder.HasIndex(e => e.Imei).HasDatabaseName("ix_home_inv_item_imei");
        builder.HasIndex(e => e.EthernetMacAddress).HasDatabaseName("ix_home_inv_item_eth_mac");
        builder.HasIndex(e => e.BluetoothMacAddress).HasDatabaseName("ix_home_inv_item_bt_mac");
        builder.HasIndex(e => e.Sku).IsUnique().HasDatabaseName("ux_home_inv_item_sku").HasFilter("sku IS NOT NULL");
    }
}