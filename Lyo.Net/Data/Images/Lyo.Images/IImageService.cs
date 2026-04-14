using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Images.Models;

namespace Lyo.Images;

/// <summary>Service interface for image processing operations.</summary>
public interface IImageService
{
    /// <summary>Gets the default image format.</summary>
    ImageFormat DefaultFormat { get; }

    /// <summary>Resizes an image.</summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="outputStream">The output image stream.</param>
    /// <param name="width">The target width in pixels.</param>
    /// <param name="height">The target height in pixels.</param>
    /// <param name="resizeMode">The resize mode (e.g., Crop, Pad, BoxPad).</param>
    /// <param name="format">The output image format. If null, uses the input format.</param>
    /// <param name="quality">The image quality (1-100, for lossy formats).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> ResizeAsync(
        Stream inputStream,
        Stream outputStream,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Crops an image to the specified rectangle.</summary>
    Task<Result<bool>> CropAsync(
        Stream inputStream,
        Stream outputStream,
        int x,
        int y,
        int width,
        int height,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Rotates an image by the specified degrees.</summary>
    Task<Result<bool>> RotateAsync(Stream inputStream, Stream outputStream, float degrees, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>Adds a watermark to an image.</summary>
    Task<Result<bool>> WatermarkAsync(
        Stream inputStream,
        Stream outputStream,
        string watermarkText,
        WatermarkOptions? options = null,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Converts an image to a different format.</summary>
    Task<Result<bool>> ConvertFormatAsync(Stream inputStream, Stream outputStream, ImageFormat targetFormat, int? quality = null, CancellationToken ct = default);

    /// <summary>Generates a thumbnail from an image.</summary>
    Task<Result<byte[]>> GenerateThumbnailAsync(Stream inputStream, int maxWidth, int maxHeight, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>
    /// Gets image metadata (dimensions, format, and optionally EXIF such as location, device, date taken, etc.). EXIF support depends on the implementation (e.g. ImageSharp
    /// provides full EXIF; Skia provides basic metadata with optional EXIF when MetadataExtractor is used).
    /// </summary>
    Task<Result<ImageMetadata>> GetMetadataAsync(Stream imageStream, CancellationToken ct = default);

    /// <summary>Gets image metadata from a file path. Convenience overload that opens the file and returns metadata.</summary>
    Task<Result<ImageMetadata>> GetMetadataFromFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Gets the dominant color palette of an image.</summary>
    /// <param name="imageStream">The input image stream.</param>
    /// <param name="colorCount">The number of colors to extract for the palette.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A palette containing up to the specified number of colors as hex strings (e.g., "#RRGGBB").</returns>
    Task<Result<ImagePalette>> GetPaletteAsync(Stream imageStream, int colorCount, CancellationToken ct = default);

    /// <summary>Compresses an image (reduces file size while maintaining quality).</summary>
    Task<Result<bool>> CompressAsync(Stream inputStream, Stream outputStream, int quality, ImageFormat? format = null, CancellationToken ct = default);

    /// <summary>Resizes an image from a file path.</summary>
    Task<Result<bool>> ResizeFileAsync(
        string inputFilePath,
        string outputFilePath,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Processes multiple images in batch.</summary>
    Task<BulkResult<ImageProcessRequest, ImageOperationResult>> ProcessBatchAsync(IEnumerable<ImageProcessRequest> requests, CancellationToken ct = default);

    /// <summary>
    /// Draws <paramref name="overlayImageBytes" /> centered on <paramref name="backgroundPng" /> (square canvas), with an optional light pad and border—typical for QR + logo.
    /// Output is PNG bytes.
    /// </summary>
    Task<Result<byte[]>> CompositeCenterOverlayPngAsync(byte[] backgroundPng, byte[] overlayImageBytes, ImageCenterOverlayOptions options, CancellationToken ct = default);

    /// <summary>
    /// Draws a decorative frame around a square QR PNG (badge, rounded panel, or stroked border). Requires <see cref="SixLabors.Fonts" /> at runtime for captioned styles.
    /// </summary>
    Task<Result<byte[]>> CompositeQrFramePngAsync(byte[] qrPng, QrFrameLayoutOptions options, CancellationToken ct = default);
}