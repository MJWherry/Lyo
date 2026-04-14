using Lyo.Common;

namespace Lyo.HomeInventory;

/// <summary>
/// Something you own at home: smart device, appliance, major purchase, etc. Tracks receipts/order refs, part numbers, barcodes, network identifiers, and warranty dates; use
/// <see cref="CustomAttributesJson" /> for extra fields.
/// </summary>
public sealed class HomeItemRecord
{
    public Guid Id { get; set; }

    /// <summary>Optional owner (e.g. Person, Account) using EntityRef components.</summary>
    public string? OwnerEntityType { get; set; }

    public string? OwnerEntityId { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>Optional parent for variants or kit components.</summary>
    public Guid? ParentItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public HomeItemStatus Status { get; set; } = HomeItemStatus.Active;

    public HomeItemCondition Condition { get; set; } = HomeItemCondition.Unknown;

    /// <summary>Stock keeping unit (unique when set).</summary>
    public string? Sku { get; set; }

    /// <summary>Vendor or internal purchase order reference.</summary>
    public string? PurchaseOrderNumber { get; set; }

    /// <summary>Sales or fulfillment order reference.</summary>
    public string? SalesOrderNumber { get; set; }

    /// <summary>OEM or brand name.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>Manufacturer part number (MPN).</summary>
    public string? ManufacturerPartNumber { get; set; }

    /// <summary>Retailer, marketplace, or distributor name.</summary>
    public string? Seller { get; set; }

    /// <summary>Vendor catalog number for this line (distinct from <see cref="Sku" />).</summary>
    public string? VendorSku { get; set; }

    /// <summary>UPC-A / GTIN-12 when applicable.</summary>
    public string? Upc { get; set; }

    public string? Ean { get; set; }

    public string? Isbn { get; set; }

    public string? ModelNumber { get; set; }

    public string? Color { get; set; }

    public string? SerialNumber { get; set; }

    public string? Imei { get; set; }

    /// <summary>Ethernet / wired MAC (e.g. AA:BB:CC:DD:EE:FF).</summary>
    public string? EthernetMacAddress { get; set; }

    public string? WifiMacAddress { get; set; }

    public string? BluetoothMacAddress { get; set; }

    public decimal? Msrp { get; set; }

    public decimal? Cost { get; set; }

    /// <summary>ISO 4217 currency code when monetary fields are used.</summary>
    public string? Currency { get; set; }

    public int? WeightGrams { get; set; }

    public int? LengthMm { get; set; }

    public int? WidthMm { get; set; }

    public int? HeightMm { get; set; }

    public DateTime? AcquiredDate { get; set; }

    public DateTime? WarrantyExpires { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country of origin.</summary>
    public string? CountryOfOrigin { get; set; }

    public string? LotNumber { get; set; }

    public string? BatchNumber { get; set; }

    /// <summary>Arbitrary JSON for extended attributes (localized names, certifications, etc.).</summary>
    public string? CustomAttributesJson { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public EntityRef? Owner => string.IsNullOrWhiteSpace(OwnerEntityType) || string.IsNullOrWhiteSpace(OwnerEntityId) ? null : new EntityRef(OwnerEntityType, OwnerEntityId);
}