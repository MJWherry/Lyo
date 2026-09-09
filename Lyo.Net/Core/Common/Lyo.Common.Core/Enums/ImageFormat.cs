using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Image encodings commonly used when processing pictures.</summary>
public enum ImageFormat
{
    /// <summary>Image format is not known or not set</summary>
    [Description("unknown")]
    Unknown,

    /// <summary>JPEG (.jpg, .jpeg)</summary>
    [Description("jpeg")]
    Jpeg,

    /// <summary>PNG (.png)</summary>
    [Description("png")]
    Png,

    /// <summary>GIF (.gif)</summary>
    [Description("gif")]
    Gif,

    /// <summary>BMP (.bmp)</summary>
    [Description("bmp")]
    Bmp,

    /// <summary>WebP (.webp)</summary>
    [Description("webp")]
    WebP,

    /// <summary>TIFF (.tif, .tiff)</summary>
    [Description("tiff")]
    Tiff,

    /// <summary>ICO (.ico)</summary>
    [Description("ico")]
    Ico,

    /// <summary>SVG (.svg). Vector XML; decoration primitives skip raster compositing and treat SVG as XML.</summary>
    [Description("svg")]
    Svg
}