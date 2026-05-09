using SixLabors.ImageSharp.Formats.Png;

namespace Lyo.Images;

/// <summary>Shared PNG encoder settings so indexed/palette output does not flatten overlays.</summary>
internal static class ImagePngEncoding
{
    /// <summary>Default compression; smaller files, slower encode.</summary>
    public static PngEncoder Truecolor { get; } = new() { CompressionLevel = PngCompressionLevel.DefaultCompression, ColorType = PngColorType.RgbWithAlpha };

    /// <summary>Faster encode at the cost of larger output (good for QR frames and overlays).</summary>
    public static PngEncoder TruecolorFast { get; } = new() { CompressionLevel = PngCompressionLevel.BestSpeed, ColorType = PngColorType.RgbWithAlpha };

    /// <summary>Selects encoder for truecolor RGBA compositing based on <see cref="Models.ImageServiceOptions.UseFastPngForQrComposites" />.</summary>
    public static PngEncoder TruecolorForComposites(bool useFast) => useFast ? TruecolorFast : Truecolor;
}
