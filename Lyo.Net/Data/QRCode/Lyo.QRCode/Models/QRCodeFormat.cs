using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Supported QR output formats.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum QRCodeFormat
{
    /// <summary>PNG raster format.</summary>
    Png,

    /// <summary>SVG vector output.</summary>
    Svg,

    /// <summary>JPEG raster format.</summary>
    Jpeg,

    /// <summary>Bitmap raster format.</summary>
    Bitmap
}