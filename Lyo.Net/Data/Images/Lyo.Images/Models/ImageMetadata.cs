using System.Diagnostics;
using Lyo.Common.Core.Enums;

namespace Lyo.Images.Models;

/// <summary>Size, format, optional file length, and optional EXIF for an inspected image.</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Format">Detected or reported <see cref="ImageFormat" />.</param>
/// <param name="FileSizeBytes">Source size when known (e.g. from a file).</param>
/// <param name="BitsPerPixel">Color depth when reported.</param>
/// <param name="HasAlpha">True when the image has an alpha channel, if known.</param>
/// <param name="ExifInfo">Structured EXIF subset when present.</param>
/// <param name="ExifData">Raw EXIF tag map when the backend exposes it.</param>
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