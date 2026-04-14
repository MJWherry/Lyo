using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Represents common image formats used for image processing.</summary>
public enum ImageFormat
{
    /// <summary>Unknown or unspecified image format</summary>
    [Description("unknown")]
    Unknown,

    /// <summary>JPEG image format (.jpg, .jpeg)</summary>
    [Description("jpeg")]
    Jpeg,

    /// <summary>PNG image format (.png)</summary>
    [Description("png")]
    Png,

    /// <summary>GIF image format (.gif)</summary>
    [Description("gif")]
    Gif,

    /// <summary>BMP image format (.bmp)</summary>
    [Description("bmp")]
    Bmp,

    /// <summary>WebP image format (.webp)</summary>
    [Description("webp")]
    WebP,

    /// <summary>TIFF image format (.tif, .tiff)</summary>
    [Description("tiff")]
    Tiff,

    /// <summary>ICO image format (.ico)</summary>
    [Description("ico")]
    Ico
}