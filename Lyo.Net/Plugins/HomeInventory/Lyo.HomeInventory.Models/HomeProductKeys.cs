namespace Lyo.HomeInventory.Models;

/// <summary>
/// Well-known identifier kinds for <see cref="HomeProductKeyEntry"/>. An item may store several rows of the same kind (two Wi-Fi MACs, two SKUs for kit parts). There
/// is no generic MAC or Ethernet kind — use <see cref="WifiMac"/> or <see cref="BluetoothMac"/>.
/// </summary>
public static class HomeProductKeys
{
    /// <summary>Stock-keeping unit.</summary>
    public const string Sku = "Sku";

    /// <summary>Manufacturer serial.</summary>
    public const string SerialNumber = "SerialNumber";

    /// <summary>Vendor or internal product code (not SKU).</summary>
    public const string ProductCode = "ProductCode";

    /// <summary>Model number.</summary>
    public const string ModelNumber = "ModelNumber";

    /// <summary>Catalog number on a vendor line.</summary>
    public const string VendorSku = "VendorSku";

    /// <summary>UPC-A / GTIN-12.</summary>
    public const string Upc = "Upc";

    /// <summary>EAN / GTIN-13.</summary>
    public const string Ean = "Ean";

    /// <summary>ISBN.</summary>
    public const string Isbn = "Isbn";

    /// <summary>IMEI for cellular devices.</summary>
    public const string Imei = "Imei";

    /// <summary>Wi-Fi radio MAC.</summary>
    public const string WifiMac = "WifiMac";

    /// <summary>Bluetooth radio MAC.</summary>
    public const string BluetoothMac = "BluetoothMac";

    /// <summary>Lot number.</summary>
    public const string LotNumber = "LotNumber";

    /// <summary>Batch number.</summary>
    public const string BatchNumber = "BatchNumber";

    /// <summary>Purchase-order reference.</summary>
    public const string PurchaseOrder = "PurchaseOrder";

    /// <summary>Sales-order reference.</summary>
    public const string SalesOrder = "SalesOrder";

    /// <summary>Known kinds in display order. Custom free-text kinds are allowed in addition to these.</summary>
    public static readonly string[] All = [
        Sku, SerialNumber, ProductCode, ModelNumber, VendorSku, Upc, Ean, Isbn, Imei, WifiMac, BluetoothMac, LotNumber, BatchNumber, PurchaseOrder, SalesOrder
    ];

    /// <summary>Short label for a known kind; unknown values are returned unchanged.</summary>
    public static string DisplayName(string kind)
        => kind switch {
            Sku => "SKU",
            SerialNumber => "Serial number",
            ProductCode => "Product code",
            ModelNumber => "Model number",
            VendorSku => "Vendor SKU",
            Upc => "UPC",
            Ean => "EAN",
            Isbn => "ISBN",
            Imei => "IMEI",
            WifiMac => "Wi-Fi MAC",
            BluetoothMac => "Bluetooth MAC",
            LotNumber => "Lot number",
            BatchNumber => "Batch number",
            PurchaseOrder => "Purchase order",
            SalesOrder => "Sales order",
            var _ => kind
        };
}
