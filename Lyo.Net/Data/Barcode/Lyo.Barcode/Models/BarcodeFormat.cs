namespace Lyo.Barcode.Models;

/// <summary>Output image format for generated barcodes.</summary>
public enum BarcodeFormat
{
    /// <summary>Windows BMP (24-bit), no external image dependencies.</summary>
    Bmp,

    /// <summary>SVG vector markup.</summary>
    Svg
}