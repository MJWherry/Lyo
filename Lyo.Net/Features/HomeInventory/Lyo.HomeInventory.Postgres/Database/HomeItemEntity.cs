using System.ComponentModel.DataAnnotations;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string? OwnerEntityType { get; set; }

    [MaxLength(200)]
    public string? OwnerEntityId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? ParentItemId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public int Status { get; set; }

    public int Condition { get; set; }

    [MaxLength(120)]
    public string? Sku { get; set; }

    [MaxLength(120)]
    public string? PurchaseOrderNumber { get; set; }

    [MaxLength(120)]
    public string? SalesOrderNumber { get; set; }

    [MaxLength(300)]
    public string? Manufacturer { get; set; }

    [MaxLength(200)]
    public string? ManufacturerPartNumber { get; set; }

    [MaxLength(300)]
    public string? Seller { get; set; }

    [MaxLength(120)]
    public string? VendorSku { get; set; }

    [MaxLength(32)]
    public string? Upc { get; set; }

    [MaxLength(32)]
    public string? Ean { get; set; }

    [MaxLength(32)]
    public string? Isbn { get; set; }

    [MaxLength(200)]
    public string? ModelNumber { get; set; }

    [MaxLength(120)]
    public string? Color { get; set; }

    [MaxLength(200)]
    public string? SerialNumber { get; set; }

    [MaxLength(64)]
    public string? Imei { get; set; }

    [MaxLength(32)]
    public string? EthernetMacAddress { get; set; }

    [MaxLength(32)]
    public string? WifiMacAddress { get; set; }

    [MaxLength(32)]
    public string? BluetoothMacAddress { get; set; }

    public decimal? Msrp { get; set; }

    public decimal? Cost { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    public int? WeightGrams { get; set; }

    public int? LengthMm { get; set; }

    public int? WidthMm { get; set; }

    public int? HeightMm { get; set; }

    public DateTime? AcquiredDate { get; set; }

    public DateTime? WarrantyExpires { get; set; }

    [MaxLength(2)]
    public string? CountryOfOrigin { get; set; }

    [MaxLength(120)]
    public string? LotNumber { get; set; }

    [MaxLength(120)]
    public string? BatchNumber { get; set; }

    public string? CustomAttributesJson { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public HomeCategoryEntity? Category { get; set; }

    public HomeItemEntity? ParentItem { get; set; }

    public List<HomeItemEntity> ChildItems { get; set; } = [];

    public List<HomeItemStockEntity> StockRows { get; set; } = [];

    public List<HomeItemMovementEntity> Movements { get; set; } = [];
}