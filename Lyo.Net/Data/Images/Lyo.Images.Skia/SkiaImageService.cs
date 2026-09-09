using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Images.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using static Lyo.Images.ImageErrorCodes;

namespace Lyo.Images.Skia;

/// <summary><see cref="IImageService" /> backed by SkiaSharp.</summary>
public class SkiaImageService : ImageServiceBase
{
    /// <summary>Builds a <see cref="SkiaImageService" />.</summary>
    /// <param name="options">Image service options.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="metrics">Optional metrics sink.</param>
    public SkiaImageService(ImageServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
        MetricNames[nameof(Images.Constants.Metrics.ResizeDuration)] = Constants.Metrics.ResizeDuration;
        MetricNames[nameof(Images.Constants.Metrics.CropDuration)] = Constants.Metrics.CropDuration;
        MetricNames[nameof(Images.Constants.Metrics.RotateDuration)] = Constants.Metrics.RotateDuration;
        MetricNames[nameof(Images.Constants.Metrics.WatermarkDuration)] = Constants.Metrics.WatermarkDuration;
        MetricNames[nameof(Images.Constants.Metrics.ConvertDuration)] = Constants.Metrics.ConvertDuration;
        MetricNames[nameof(Images.Constants.Metrics.ThumbnailDuration)] = Constants.Metrics.ThumbnailDuration;
        MetricNames[nameof(Images.Constants.Metrics.MetadataDuration)] = Constants.Metrics.MetadataDuration;
        MetricNames[nameof(Images.Constants.Metrics.PaletteDuration)] = Constants.Metrics.PaletteDuration;
        MetricNames[nameof(Images.Constants.Metrics.CompressDuration)] = Constants.Metrics.CompressDuration;
        MetricNames[nameof(Images.Constants.Metrics.BatchProcessDuration)] = Constants.Metrics.BatchProcessDuration;
    }

    /// <summary>Resizes an image.</summary>
    public override async Task<Result<bool>> ResizeAsync(
        Stream inputStream,
        Stream outputStream,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNotInRange(width, 1, int.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(height, 1, int.MaxValue);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.ResizeDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        var (newWidth, newHeight) = CalculateResizeDimensions(bitmap.Width, bitmap.Height, width, height, resizeMode);
                        using var resizedBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                        OperationHelpers.ThrowIfNull(resizedBitmap, "Failed to resize image");
                        SaveBitmap(resizedBitmap, outputStream, format ?? DefaultFormat, quality ?? Options.DefaultQuality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("Resize", $"Image resized to {width}x{height}");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "Resize", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to resize image to {Width}x{Height}", width, height);
            return ImageOperationResult.FromException(ex, "Resize", ResizeFailed);
        }
    }

    /// <summary>Crops an image to the given rectangle.</summary>
    public new async Task<Result<bool>> CropAsync(
        Stream inputStream,
        Stream outputStream,
        int x,
        int y,
        int width,
        int height,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNotInRange(x, 0, int.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(y, 0, int.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(width, 1, int.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(height, 1, int.MaxValue);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.CropDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        // Clamp the crop rectangle to image bounds
                        var cropX = Math.Max(0, Math.Min(x, bitmap.Width - 1));
                        var cropY = Math.Max(0, Math.Min(y, bitmap.Height - 1));
                        var cropWidth = Math.Min(width, bitmap.Width - cropX);
                        var cropHeight = Math.Min(height, bitmap.Height - cropY);
                        using var croppedBitmap = new SKBitmap(cropWidth, cropHeight);
                        OperationHelpers.ThrowIf(!bitmap.ExtractSubset(croppedBitmap, new(cropX, cropY, cropX + cropWidth, cropY + cropHeight)), "Failed to crop image");
                        SaveBitmap(croppedBitmap, outputStream, format ?? DefaultFormat, quality ?? Options.DefaultQuality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("Crop", $"Image cropped at ({x}, {y}) with size {width}x{height}");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "Crop", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to crop image at ({X}, {Y}) with size {Width}x{Height}", x, y, width, height);
            return ImageOperationResult.FromException(ex, "Crop", CropFailed);
        }
    }

    /// <summary>Rotates an image by the given angle in degrees.</summary>
    public new async Task<Result<bool>> RotateAsync(
        Stream inputStream,
        Stream outputStream,
        float degrees,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.RotateDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
                        OperationHelpers.ThrowIfNull(surface, "Failed to create surface");
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.Transparent);
                        canvas.Translate(bitmap.Width / 2f, bitmap.Height / 2f);
                        canvas.RotateDegrees(degrees);
                        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
                        canvas.DrawBitmap(bitmap, 0, 0);
                        using var rotatedImage = surface.Snapshot();
                        SaveImage(rotatedImage, outputStream, format ?? DefaultFormat, quality ?? Options.DefaultQuality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("Rotate", $"Image rotated by {degrees} degrees");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "Rotate", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to rotate image by {Degrees} degrees", degrees);
            return ImageOperationResult.FromException(ex, "Rotate", RotateFailed);
        }
    }

    /// <inheritdoc />
    public override async Task<Result<bool>> WatermarkAsync(
        Stream inputStream,
        Stream outputStream,
        string watermarkText,
        WatermarkOptions? options = null,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(watermarkText);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.WatermarkDuration)]);
        ct.ThrowIfCancellationRequested();
        var opts = options ?? new WatermarkOptions();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        using var surface = SKSurface.Create(bitmap.Info);
                        OperationHelpers.ThrowIfNull(surface, "Failed to create surface");
                        var canvas = surface.Canvas;
                        canvas.DrawBitmap(bitmap, 0, 0);

                        // Paint used to draw the watermark string
                        var baseColor = ParseColor(opts.TextColor);
                        var alpha = (byte)(opts.Opacity * 255);
                        using var paint = new SKPaint {
                            Color = new(baseColor.Red, baseColor.Green, baseColor.Blue, alpha),
                            TextSize = opts.FontSize,
                            IsAntialias = true,
                            Typeface = SKTypeface.FromFamilyName(opts.FontFamily)
                        };

                        // Resolve where the watermark string sits
                        var textBounds = new SKRect();
                        paint.MeasureText(watermarkText, ref textBounds);
                        var (x, y) = CalculateWatermarkPosition(bitmap.Width, bitmap.Height, textBounds, opts.Position, opts.Padding);
                        canvas.DrawText(watermarkText, x, y, paint);
                        using var watermarkedImage = surface.Snapshot();
                        SaveImage(watermarkedImage, outputStream, format ?? DefaultFormat, quality ?? Options.DefaultQuality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("Watermark", $"Watermark '{watermarkText}' added successfully");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "Watermark", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to add watermark to image");
            return ImageOperationResult.FromException(ex, "Watermark", WatermarkFailed);
        }
    }

    /// <summary>Re-encodes an image into another format.</summary>
    public new async Task<Result<bool>> ConvertFormatAsync(Stream inputStream, Stream outputStream, ImageFormat targetFormat, int? quality = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.ConvertDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        SaveBitmap(bitmap, outputStream, targetFormat, quality ?? Options.DefaultQuality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("ConvertFormat", $"Image converted to {targetFormat}");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "ConvertFormat", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to convert image to {Format}", targetFormat);
            return ImageOperationResult.FromException(ex, "ConvertFormat", ConvertFormatFailed);
        }
    }

    /// <summary>Builds a thumbnail from an image.</summary>
    public new async Task<Result<byte[]>> GenerateThumbnailAsync(
        Stream inputStream,
        int maxWidth,
        int maxHeight,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNotInRange(maxWidth, 1, int.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(maxHeight, 1, int.MaxValue);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.ThumbnailDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            var thumbnailBytes = await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        var (newWidth, newHeight) = CalculateResizeDimensions(bitmap.Width, bitmap.Height, maxWidth, maxHeight, ResizeMode.Max);
                        using var resizedBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                        OperationHelpers.ThrowIfNull(resizedBitmap, "Failed to resize image");
                        using var outputStream = new MemoryStream();
                        SaveBitmap(resizedBitmap, outputStream, format ?? DefaultFormat, quality ?? Options.DefaultQuality);
                        return outputStream.ToArray();
                    }, ct)
                .ConfigureAwait(false);

            return ImageThumbnailResult.FromSuccess(thumbnailBytes, maxWidth, maxHeight, format ?? DefaultFormat, $"Thumbnail generated with max size {maxWidth}x{maxHeight}");
        }
        catch (OperationCanceledException ex) {
            return ImageThumbnailResult.FromException(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to generate thumbnail with max size {MaxWidth}x{MaxHeight}", maxWidth, maxHeight);
            return ImageThumbnailResult.FromException(ex, GenerateThumbnailFailed);
        }
    }

    /// <summary>Dominant colors sampled from an image.</summary>
    public new async Task<Result<ImagePalette>> GetPaletteAsync(Stream imageStream, int colorCount, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageStream);
        OperationHelpers.ThrowIfNotReadable(imageStream, $"Stream '{nameof(imageStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNotInRange(colorCount, 1, 256);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.PaletteDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            var ignoreTransparent = Options.IgnoreTransparentPixelsInPalette;
            var alphaCutoff = (byte)Math.Clamp(Options.PaletteAlphaCutoff, 0, 255);
            var palette = await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(imageStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        // Downscale large rasters before counting colors
                        const int maxDimension = 200;
                        int width = bitmap.Width, height = bitmap.Height;
                        if (width > maxDimension || height > maxDimension) {
                            var ratio = Math.Min((float)maxDimension / width, (float)maxDimension / height);
                            width = (int)(width * ratio);
                            height = (int)(height * ratio);
                            using var resized = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
                            OperationHelpers.ThrowIfNull(resized, "Failed to resize image for palette extraction");
                            return ExtractPalette(resized, colorCount, ignoreTransparent, alphaCutoff, ct);
                        }

                        return ExtractPalette(bitmap, colorCount, ignoreTransparent, alphaCutoff, ct);
                    }, ct)
                .ConfigureAwait(false);

            return ImagePaletteResult.FromSuccess(palette, $"Extracted {palette.Colors.Count} colors from image palette");
        }
        catch (OperationCanceledException ex) {
            return ImagePaletteResult.FromException(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to get image palette with {ColorCount} colors", colorCount);
            return ImagePaletteResult.FromException(ex, GetPaletteFailed);
        }
    }

    /// <summary>Builds a palette from a bitmap by bucketing colors and ranking by frequency.</summary>
    private static ImagePalette ExtractPalette(SKBitmap bitmap, int colorCount, bool ignoreTransparent, byte alphaCutoff, CancellationToken ct)
    {
        var colorCounts = new Dictionary<uint, int>();
        var step = Math.Max(1, bitmap.Width * bitmap.Height / 10000);
        for (var y = 0; y < bitmap.Height; y++) {
            ct.ThrowIfCancellationRequested();
            for (var x = 0; x < bitmap.Width; x += step) {
                var color = bitmap.GetPixel(x, y);
                if (ignoreTransparent && color.Alpha < alphaCutoff)
                    continue;

                var bucket = (uint)(((color.Red >> 3) << 10) | ((color.Green >> 3) << 5) | (color.Blue >> 3));
                colorCounts.TryGetValue(bucket, out var count);
                colorCounts[bucket] = count + 1;
            }
        }

        var colors = colorCounts.OrderByDescending(kv => kv.Value)
            .Take(colorCount)
            .Select(kv => {
                var b = kv.Key & 0x1F;
                var g = (kv.Key >> 5) & 0x1F;
                var r = (kv.Key >> 10) & 0x1F;
                return $"#{Expand5BitChannelTo8(r):X2}{Expand5BitChannelTo8(g):X2}{Expand5BitChannelTo8(b):X2}";
            })
            .ToList();

        return new(colors);
    }

    /// <summary>Expands a 5-bit palette bucket (0–31) to an 8-bit channel (e.g. for hex display).</summary>
    private static int Expand5BitChannelTo8(uint fiveBits) => (int)((fiveBits << 3) | (fiveBits >> 2));

    /// <summary>Image metadata: size, format, and EXIF when present.</summary>
    public new async Task<Result<ImageMetadata>> GetMetadataAsync(Stream imageStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageStream);
        OperationHelpers.ThrowIfNotReadable(imageStream, $"Stream '{nameof(imageStream)}' must be readable.");
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.MetadataDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            var metadata = await Task.Run(
                    () => {
                        var fileSize = GetStreamLength(imageStream);
                        if (imageStream.CanSeek)
                            imageStream.Position = 0;

                        using var codec = SKCodec.Create(imageStream);
                        OperationHelpers.ThrowIfNull(codec, "Could not identify image format");
                        var info = codec.Info;
                        var format = DetectFormat(codec.EncodedFormat);
                        var hasAlpha = info.AlphaType != SKAlphaType.Opaque;
                        var bitsPerPixel = GetBitsPerPixel(info.ColorType);
                        ImageExifInfo? exifInfo = null;
                        Dictionary<string, string>? exifData = null;
                        if (imageStream.CanSeek) {
                            imageStream.Position = 0;
                            (exifInfo, exifData) = SkiaExifExtractor.Extract(imageStream);
                        }

                        return new ImageMetadata(info.Width, info.Height, format, fileSize, bitsPerPixel, hasAlpha, exifInfo, exifData);
                    }, ct)
                .ConfigureAwait(false);

            return ImageMetadataResult.FromSuccess(metadata, $"Image metadata retrieved: {metadata.Width}x{metadata.Height}, Format: {metadata.Format}");
        }
        catch (OperationCanceledException ex) {
            return ImageMetadataResult.FromException(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to get image metadata");
            return ImageMetadataResult.FromException(ex, GetMetadataFailed);
        }
    }

    /// <summary>Re-encodes an image at a lower quality to shrink the file.</summary>
    public new async Task<Result<bool>> CompressAsync(Stream inputStream, Stream outputStream, int quality, ImageFormat? format = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNotInRange(quality, 1, 100);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Images.Constants.Metrics.CompressDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        using var bitmap = SKBitmap.Decode(inputStream);
                        OperationHelpers.ThrowIfNull(bitmap, "Failed to decode image");
                        SaveBitmap(bitmap, outputStream, format ?? DefaultFormat, quality);
                    }, ct)
                .ConfigureAwait(false);

            return ImageOperationResult.FromSuccess("Compress", $"Image compressed with quality {quality}");
        }
        catch (OperationCanceledException ex) {
            return ImageOperationResult.FromException(ex, "Compress", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to compress image with quality {Quality}", quality);
            return ImageOperationResult.FromException(ex, "Compress", CompressFailed);
        }
    }

    /// <summary>Resolves output width and height from the resize mode.</summary>
    protected static (int width, int height) CalculateResizeDimensions(int originalWidth, int originalHeight, int targetWidth, int targetHeight, ResizeMode mode)
        => mode switch {
            ResizeMode.Max => CalculateMaxFit(originalWidth, originalHeight, targetWidth, targetHeight),
            ResizeMode.Crop => (targetWidth, targetHeight),
            ResizeMode.Pad => (targetWidth, targetHeight),
            ResizeMode.BoxPad => (targetWidth, targetHeight),
            ResizeMode.Stretch => (targetWidth, targetHeight),
            var _ => CalculateMaxFit(originalWidth, originalHeight, targetWidth, targetHeight)
        };

    /// <summary>Fits inside the target box without changing aspect ratio.</summary>
    private static (int width, int height) CalculateMaxFit(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
    {
        var ratio = Math.Min((float)targetWidth / originalWidth, (float)targetHeight / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    /// <summary>Writes a bitmap to a stream using the given format.</summary>
    protected void SaveBitmap(SKBitmap bitmap, Stream stream, ImageFormat format, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        SaveImage(image, stream, format, quality);
    }

    /// <summary>Writes an image to a stream using the given format.</summary>
    protected void SaveImage(SKImage image, Stream stream, ImageFormat format, int quality)
    {
        var encodedFormat = GetSkiaEncodedFormat(format);
        var data = image.Encode(encodedFormat, quality);
        OperationHelpers.ThrowIfNull(data, $"Failed to encode image as {format}");
        data.SaveTo(stream);
    }

    /// <summary>SkiaSharp encoded format that matches the image format.</summary>
    protected static SKEncodedImageFormat GetSkiaEncodedFormat(ImageFormat format)
        => format switch {
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Png => SKEncodedImageFormat.Png,
            ImageFormat.WebP => SKEncodedImageFormat.Webp,
            ImageFormat.Gif => SKEncodedImageFormat.Gif,
            ImageFormat.Bmp => SKEncodedImageFormat.Bmp,
            ImageFormat.Ico => SKEncodedImageFormat.Ico,
            ImageFormat.Tiff => SKEncodedImageFormat.Png, // SkiaSharp has no TIFF encoder; fall back to PNG
            var _ => SKEncodedImageFormat.Jpeg
        };

    /// <summary>Maps a SkiaSharp encoded format onto <see cref="ImageFormat" />.</summary>
    protected static ImageFormat DetectFormat(SKEncodedImageFormat format)
        => format switch {
            SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg,
            SKEncodedImageFormat.Png => ImageFormat.Png,
            SKEncodedImageFormat.Webp => ImageFormat.WebP,
            SKEncodedImageFormat.Gif => ImageFormat.Gif,
            SKEncodedImageFormat.Bmp => ImageFormat.Bmp,
            SKEncodedImageFormat.Ico => ImageFormat.Ico,
            var _ => ImageFormat.Jpeg
        };

    /// <summary>Parses a hex color string into an <c>SKColor</c>.</summary>
    protected static SKColor ParseColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return SKColors.White;

        hexColor = hexColor.TrimStart('#');
        if (hexColor.Length == 6) {
            var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
            var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
            var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
            return new(r, g, b);
        }

        return SKColors.White;
    }

    /// <summary>Resolves watermark coordinates from the position enum.</summary>
    protected static (float x, float y) CalculateWatermarkPosition(int imageWidth, int imageHeight, SKRect textBounds, WatermarkPosition position, int padding)
    {
        var textWidth = textBounds.Width;
        var textHeight = textBounds.Height;
        return position switch {
            WatermarkPosition.TopLeft => (padding, textHeight + padding),
            WatermarkPosition.TopRight => (imageWidth - textWidth - padding, textHeight + padding),
            WatermarkPosition.BottomLeft => (padding, imageHeight - padding),
            WatermarkPosition.BottomRight => (imageWidth - textWidth - padding, imageHeight - padding),
            WatermarkPosition.Center => ((imageWidth - textWidth) / 2f, (imageHeight + textHeight) / 2f),
            var _ => (imageWidth - textWidth - padding, imageHeight - padding)
        };
    }

    /// <summary>Channel depth in bits for a color type.</summary>
    protected static int GetBitsPerPixel(SKColorType colorType)
        => colorType switch {
            SKColorType.Unknown => 0,
            SKColorType.Alpha8 => 8,
            SKColorType.Rgb565 => 16,
            SKColorType.Argb4444 => 16,
            SKColorType.Rgba8888 => 32,
            SKColorType.Rgb888x => 32,
            SKColorType.Bgra8888 => 32,
            SKColorType.Rgba1010102 => 32,
            SKColorType.Rgb101010x => 32,
            SKColorType.Gray8 => 8,
            SKColorType.RgbaF16 => 64,
            SKColorType.RgbaF16Clamped => 64,
            SKColorType.RgbaF32 => 128,
            SKColorType.Rg88 => 16,
            SKColorType.AlphaF16 => 16,
            SKColorType.RgF16 => 32,
            SKColorType.Alpha16 => 16,
            SKColorType.Rg1616 => 32,
            SKColorType.Rgba16161616 => 64,
            var _ => 32 // Default to 32 bits per pixel
        };
}