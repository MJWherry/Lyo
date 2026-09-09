using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Image type produced when a barcode is rendered.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum BarcodeFormat
{
    /// <summary>24-bit Windows BMP. No extra image libraries are required.</summary>
    Bmp,

    /// <summary>Vector markup in SVG.</summary>
    Svg
}