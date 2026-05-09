using System.Diagnostics;
using Lyo.Common.Enums;

namespace Lyo.Images.Models;

/// <summary>Dimensions, format, optional file size, and optional EXIF for an inspected image.</summary>
/// <param name="Width">Pixel width.</param>
/// <param name="Height">Pixel height.</param>
/// <param name="Format">Detected or reported <see cref="ImageFormat" />.</param>
/// <param name="FileSizeBytes">Source size when known (e.g. from file).</param>
/// <param name="BitsPerPixel">Color depth when reported.</param>
/// <param name="HasAlpha">Whether the image has an alpha channel when known.</param>
/// <param name="ExifInfo">Structured EXIF subset when available.</param>
/// <param name="ExifData">Raw EXIF tag map when implementations expose it.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record ImageMetadata(
    int Width,
    int Height,
    ImageFormat Format,
    long? FileSizeBytes,
    int? BitsPerPixel = null,
    bool? HasAlpha = null,
    ImageExifInfo? ExifInfo = null,
    Dictionary<string, string>? ExifData = null)
{
    public override string ToString() => $"{Width}x{Height} {Format}" + (FileSizeBytes.HasValue ? $" ({FileSizeBytes} bytes)" : "");
}