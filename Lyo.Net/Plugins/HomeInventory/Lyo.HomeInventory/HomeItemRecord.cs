using Lyo.Common.Core.Extensions;
using Lyo.EntityReference.Models;

namespace Lyo.HomeInventory;

/// <summary>
/// A household item: device, appliance, major purchase, and similar. Tracks receipts/order refs, part numbers, barcodes, network ids, and warranty dates; use
/// <see cref="CustomAttributesJson" /> for additional fields.
/// </summary>
public sealed class HomeItemRecord
{
    public Guid Id { get; set; }

    /// <summary>owner (e.g. Person, Account) using EntityRef components. Present only when supplied.</summary>
    public string? OwnerEntityType { get; set; }

    public string? OwnerEntityId { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>parent for variants or kit components. Present only when supplied.</summary>
    public Guid? ParentItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public HomeItemStatus Status { get; set; } = HomeItemStatus.Active;

    public HomeItemCondition Condition { get; set; } = HomeItemCondition.Unknown;

    /// <summary>SKU; unique when present.</summary>
    public string? Sku { get; set; }

    /// <summary>PO reference (vendor or internal).</summary>
    public string? PurchaseOrderNumber { get; set; }

    /// <summary>Sales/fulfillment order ref.</summary>
    public string? SalesOrderNumber { get; set; }

    /// <summary>Brand or OEM name.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>Manufacturer part number / MPN.</summary>
    public string? ManufacturerPartNumber { get; set; }

    /// <summary>Name of the retailer, marketplace, or distributor.</summary>
    public string? Seller { get; set; }

    /// <summary>Catalog number on this line (distinct from <see cref="Sku" />).</summary>
    public string? VendorSku { get; set; }

    /// <summary>UPC-A or GTIN-12 when one exists.</summary>
    public string? Upc { get; set; }

    public string? Ean { get; set; }

    public string? Isbn { get; set; }

    public string? ModelNumber { get; set; }

    public string? Color { get; set; }

    public string? SerialNumber { get; set; }

    public string? Imei { get; set; }

    /// <summary>Wired Ethernet MAC (e.g. AA:BB:CC:DD:EE:FF).</summary>
    public string? EthernetMacAddress { get; set; }

    public string? WifiMacAddress { get; set; }

    public string? BluetoothMacAddress { get; set; }

    public decimal? Msrp { get; set; }

    public decimal? Cost { get; set; }

    /// <summary>ISO 4217 currency when monetary fields are used.</summary>
    public string? Currency { get; set; }

    public int? WeightGrams { get; set; }

    public int? LengthMm { get; set; }

    public int? WidthMm { get; set; }

    public int? HeightMm { get; set; }

    public DateTime? AcquiredDate { get; set; }

    public DateTime? WarrantyExpires { get; set; }

    /// <summary>ISO 3166-1 alpha-2 origin country.</summary>
    public string? CountryOfOrigin { get; set; }

    public string? LotNumber { get; set; }

    public string? BatchNumber { get; set; }

    /// <summary>Free-form JSON for extended attributes (localized names, certifications, etc.).</summary>
    public string? CustomAttributesJson { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public EntityRef? Owner => OwnerEntityType.IsNullOrWhitespace() || OwnerEntityId.IsNullOrWhitespace() ? null : new EntityRef(OwnerEntityType, OwnerEntityId);
}