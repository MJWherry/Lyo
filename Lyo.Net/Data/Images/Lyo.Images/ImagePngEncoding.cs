using SixLabors.ImageSharp.Formats.Png;

namespace Lyo.Images;

/// <summary>Shared PNG encoder settings so indexed/palette output does not flatten overlays.</summary>
internal static class ImagePngEncoding
{
    public static PngEncoder Truecolor { get; } = new() {
        CompressionLevel = PngCompressionLevel.DefaultCompression,
        ColorType = PngColorType.RgbWithAlpha
    };
}
