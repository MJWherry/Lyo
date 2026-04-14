using System.Diagnostics;
using Lyo.Common.Enums;

namespace Lyo.Images.Models;

/// <summary>Represents image metadata.</summary>
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