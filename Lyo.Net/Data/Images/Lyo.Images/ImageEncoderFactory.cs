using Lyo.Common.Enums;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Lyo.Images;

/// <summary>Selects an ImageSharp <see cref="IImageEncoder" /> for a logical <see cref="ImageFormat" />, applying quality for lossy formats.</summary>
internal static class ImageEncoderFactory
{
    /// <summary>Returns an encoder appropriate for <paramref name="format" />. Unsupported formats throw <see cref="NotSupportedException" />.</summary>
    public static IImageEncoder GetEncoder(ImageFormat format, int? quality)
        => format switch {
            ImageFormat.Jpeg => new JpegEncoder { Quality = quality ?? 90 },
            ImageFormat.Png => ImagePngEncoding.Truecolor,
            ImageFormat.WebP => throw new NotSupportedException("WebP format requires additional package"),
            ImageFormat.Gif => throw new NotSupportedException("GIF format requires additional package"),
            ImageFormat.Bmp => throw new NotSupportedException("BMP format requires additional package"),
            ImageFormat.Tiff => throw new NotSupportedException("TIFF format requires additional package"),
            ImageFormat.Ico => throw new NotSupportedException("ICO format requires additional package"),
            var _ => new JpegEncoder { Quality = quality ?? 90 }
        };
}