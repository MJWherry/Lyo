using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Supported QR code output formats.</summary>
[DebuggerDisplay("QRCodeFormat.{ToString()}")]
public enum QRCodeFormat
{
    /// <summary>PNG image format.</summary>
    Png,

    /// <summary>SVG vector format.</summary>
    Svg,

    /// <summary>JPEG image format.</summary>
    Jpeg,

    /// <summary>Bitmap format.</summary>
    Bitmap
}