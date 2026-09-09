using Lyo.Common.Core.Enums;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>
/// Image-processing service. Extends <see cref="IImageDecorationService" />, so callers that take <see cref="IImageService" /> also get overlay, frame, caption, and padding.
/// </summary>
public interface IImageService : IImageDecorationService
{
    /// <summary>Default image format.</summary>
    ImageFormat DefaultFormat { get; }

    /// <summary>Resizes an image.</summary>
    /// <param name="inputStream">Source image stream.</param>
    /// <param name="outputStream">Destination image stream.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="resizeMode">Resize mode (e.g., Crop, Pad, BoxPad).</param>
    /// <param name="format">Output format. Null keeps the input format.</param>
    /// <param name="quality">Quality 1-100 for lossy formats.</param>
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

    /// <summary>Crops the image to the given rectangle.</summary>
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

    /// <summary>Rotates the image by the given degrees.</summary>
    Task<Result<bool>> RotateAsync(Stream inputStream, Stream outputStream, float degrees, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>Draws a watermark on the image.</summary>
    Task<Result<bool>> WatermarkAsync(
        Stream inputStream,
        Stream outputStream,
        string watermarkText,
        WatermarkOptions? options = null,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Converts the image to another format.</summary>
    Task<Result<bool>> ConvertFormatAsync(Stream inputStream, Stream outputStream, ImageFormat targetFormat, int? quality = null, CancellationToken ct = default);

    /// <summary>Builds a thumbnail from the image.</summary>
    Task<Result<byte[]>> GenerateThumbnailAsync(Stream inputStream, int maxWidth, int maxHeight, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>
    /// Image metadata (dimensions, format, and optional EXIF: location, device, date taken, and similar). EXIF coverage depends on the backend (ImageSharp has full EXIF; Skia has
    /// basic metadata, plus EXIF when MetadataExtractor is wired).
    /// </summary>
    Task<Result<ImageMetadata>> GetMetadataAsync(Stream imageStream, CancellationToken ct = default);

    /// <summary>Image metadata from a file path. Opens the file and returns metadata.</summary>
    Task<Result<ImageMetadata>> GetMetadataFromFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Dominant color palette of the image.</summary>
    /// <param name="imageStream">Source image stream.</param>
    /// <param name="colorCount">How many colors to extract.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Palette of up to that many colors as hex strings (e.g., "#RRGGBB").</returns>
    Task<Result<ImagePalette>> GetPaletteAsync(Stream imageStream, int colorCount, CancellationToken ct = default);

    /// <summary>Compresses the image (smaller file, quality kept as requested).</summary>
    Task<Result<bool>> CompressAsync(Stream inputStream, Stream outputStream, int quality, ImageFormat? format = null, CancellationToken ct = default);

    /// <summary>Resizes an image on disk.</summary>
    Task<Result<bool>> ResizeFileAsync(
        string inputFilePath,
        string outputFilePath,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Processes many images as a batch.</summary>
    Task<BulkResult<ImageProcessRequest, ImageOperationResult>> ProcessBatchAsync(IEnumerable<ImageProcessRequest> requests, CancellationToken ct = default);
}