using System.Diagnostics;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Images.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using ImageSharpResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ResizeMode = Lyo.Images.Models.ResizeMode;
using static Lyo.Images.ImageErrorCodes;

namespace Lyo.Images;

/// <summary>Base implementation of IImageService using ImageSharp.</summary>
public abstract class ImageServiceBase : IImageService
{
    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the metrics instance.</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Gets the image service options.</summary>
    protected ImageServiceOptions Options { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Initializes a new instance of the <see cref="ImageServiceBase" /> class.</summary>
    /// <param name="options">The image service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance.</param>
    protected ImageServiceBase(ImageServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <inheritdoc />
    public ImageFormat DefaultFormat => ImageFormat.Jpeg;

    /// <inheritdoc />
    public virtual async Task<Result<bool>> ResizeAsync(
        Stream inputStream,
        Stream outputStream,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNotInRange(width, 1, int.MaxValue, nameof(width));
        ArgumentHelpers.ThrowIfNotInRange(height, 1, int.MaxValue, nameof(height));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.ResizeDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            var resizeOptions = new ResizeOptions { Size = new(width, height), Mode = ConvertResizeMode(resizeMode) };
            image.Mutate(x => x.Resize(resizeOptions));
            var encoder = GetEncoder(format ?? DefaultFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
            return ImageOperationResult.FromSuccess("Resize", $"Image resized to {width}x{height}");
        }
        catch (OperationCanceledException ex) {
            Logger.LogWarning(ex, "Image resize operation was cancelled");
            return ImageOperationResult.FromException(ex, "Resize", OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to resize image to {Width}x{Height}", width, height);
            return ImageOperationResult.FromException(ex, "Resize", ResizeFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CropAsync(
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
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNotInRange(x, 0, int.MaxValue, nameof(x));
        ArgumentHelpers.ThrowIfNotInRange(y, 0, int.MaxValue, nameof(y));
        ArgumentHelpers.ThrowIfNotInRange(width, 1, int.MaxValue, nameof(width));
        ArgumentHelpers.ThrowIfNotInRange(height, 1, int.MaxValue, nameof(height));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.CropDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            var rectangle = new Rectangle(x, y, width, height);
            image.Mutate(x => x.Crop(rectangle));
            var encoder = GetEncoder(format ?? DefaultFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<Result<bool>> RotateAsync(
        Stream inputStream,
        Stream outputStream,
        float degrees,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.RotateDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            image.Mutate(x => x.Rotate(degrees));
            var encoder = GetEncoder(format ?? DefaultFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
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
    public async Task<Result<bool>> WatermarkAsync(
        Stream inputStream,
        Stream outputStream,
        string watermarkText,
        WatermarkOptions? options = null,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(watermarkText, nameof(watermarkText));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.WatermarkDuration)]);
        ct.ThrowIfCancellationRequested();

        // Watermark implementation would require font loading - simplified for base class
        Logger.LogWarning("Watermark functionality requires font loading - implement in derived class");
        var ex = new NotImplementedException("Watermark functionality should be implemented in derived class with font support");
        return ImageOperationResult.FromException(ex, "Watermark", WatermarkFailed);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertFormatAsync(Stream inputStream, Stream outputStream, ImageFormat targetFormat, int? quality = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.ConvertDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            var encoder = GetEncoder(targetFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<Result<byte[]>> GenerateThumbnailAsync(
        Stream inputStream,
        int maxWidth,
        int maxHeight,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNotInRange(maxWidth, 1, int.MaxValue, nameof(maxWidth));
        ArgumentHelpers.ThrowIfNotInRange(maxHeight, 1, int.MaxValue, nameof(maxHeight));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.ThumbnailDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            var resizeOptions = new ResizeOptions { Size = new(maxWidth, maxHeight), Mode = ImageSharpResizeMode.Max };
            image.Mutate(x => x.Resize(resizeOptions));
            using var outputStream = new MemoryStream();
            var encoder = GetEncoder(format ?? DefaultFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
            var thumbnailBytes = outputStream.ToArray();
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

    /// <inheritdoc />
    public async Task<Result<byte[]>> CompositeCenterOverlayPngAsync(
        byte[] backgroundPng,
        byte[] overlayImageBytes,
        ImageCenterOverlayOptions options,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(backgroundPng, nameof(backgroundPng));
        ArgumentHelpers.ThrowIfNull(overlayImageBytes, nameof(overlayImageBytes));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        var overlayPct = Math.Clamp(options.OverlaySizePercent, 1, 50);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.CompositeOverlayDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            await using var bgStream = new MemoryStream(backgroundPng, false);
            using var qr = await Image.LoadAsync<Rgba32>(bgStream, ct).ConfigureAwait(false);
            if (options.BackgroundSquareSize is > 0) {
                var s = options.BackgroundSquareSize.Value;
                if (qr.Width != s || qr.Height != s)
                    qr.Mutate(x => x.Resize(s, s));
            }

            var w = qr.Width;
            var h = qr.Height;
            var side = Math.Min(w, h);
            var iconSize = Math.Max(1, (int)(side * (overlayPct / 100.0)));
            var ix = (w - iconSize) / 2;
            var iy = (h - iconSize) / 2;
            if (!Color.TryParse(options.BorderColorHex, out var light))
                light = Color.White;

            await using var iconStream = new MemoryStream(overlayImageBytes, false);
            using var iconImg = await Image.LoadAsync<Rgba32>(iconStream, ct).ConfigureAwait(false);
            iconImg.Mutate(x => x.Resize(new ResizeOptions { Size = new(iconSize, iconSize), Mode = ImageSharpResizeMode.Pad, PadColor = Color.Transparent }));
            qr.Mutate(ctx => {
                // Clamp pad/border to canvas — small QR sizes (e.g. under 8 px) made ix-2 negative and broke drawing.
                var padL = Math.Max(0, ix - 2);
                var padT = Math.Max(0, iy - 2);
                var padR = Math.Min(w, ix + iconSize + 2);
                var padB = Math.Min(h, iy + iconSize + 2);
                if (padR > padL && padB > padT)
                    ctx.Fill(light, new RectangularPolygon(padL, padT, padR - padL, padB - padT));

                ctx.DrawImage(iconImg, new Point(ix, iy), 1f);
                if (options.DrawOverlayBorder) {
                    var bL = Math.Max(0, ix - 1);
                    var bT = Math.Max(0, iy - 1);
                    var bR = Math.Min(w, ix + iconSize + 1);
                    var bB = Math.Min(h, iy + iconSize + 1);
                    if (bR > bL && bB > bT)
                        ctx.Draw(Pens.Solid(light, 2), new RectangularPolygon(bL, bT, bR - bL, bB - bT));
                }
            });

            await using var ms = new MemoryStream();
            await qr.SaveAsync(ms, ImagePngEncoding.Truecolor, ct).ConfigureAwait(false);
            return Result<byte[]>.Success(ms.ToArray());
        }
        catch (OperationCanceledException ex) {
            Logger.LogWarning(ex, "Center overlay operation was cancelled");
            return Result<byte[]>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to composite center overlay");
            return Result<byte[]>.Failure(ex, CompositeOverlayFailed);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Result<byte[]>> CompositeQrFramePngAsync(byte[] qrPng, QrFrameLayoutOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(qrPng, nameof(qrPng));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ct.ThrowIfCancellationRequested();
        try {
            var bytes = await QrFrameLayoutCompositor.ApplyAsync(qrPng, options, ct).ConfigureAwait(false);
            return Result<byte[]>.Success(bytes);
        }
        catch (OperationCanceledException ex) {
            Logger.LogWarning(ex, "QR frame composite was cancelled");
            return Result<byte[]>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to composite QR frame");
            return Result<byte[]>.Failure(ex, QrFrameCompositeFailed);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Result<ImagePalette>> GetPaletteAsync(Stream imageStream, int colorCount, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageStream, nameof(imageStream));
        OperationHelpers.ThrowIfNotReadable(imageStream, $"Stream '{nameof(imageStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNotInRange(colorCount, 1, 256, nameof(colorCount));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.PaletteDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync<Rgba32>(imageStream, ct).ConfigureAwait(false);
            // Resize for performance when processing large images
            const int maxDimension = 200;
            if (image.Width > maxDimension || image.Height > maxDimension)
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new(maxDimension, maxDimension), Mode = ImageSharpResizeMode.Max }));

            var quantizer = new OctreeQuantizer(new() { MaxColors = colorCount });
            image.Mutate(x => x.Quantize(quantizer));
            var colorHistogram = new Dictionary<string, int>(StringComparer.Ordinal);
            var ignoreTransparent = Options.IgnoreTransparentPixelsInPalette;
            var alphaCutoff = (byte)Math.Clamp(Options.PaletteAlphaCutoff, 0, 255);
            image.ProcessPixelRows(accessor => {
                for (var y = 0; y < accessor.Height; y++) {
                    ct.ThrowIfCancellationRequested();
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++) {
                        var pixel = row[x];
                        if (ignoreTransparent && pixel.A < alphaCutoff)
                            continue;

                        var hex = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}";
                        colorHistogram.TryGetValue(hex, out var count);
                        colorHistogram[hex] = count + 1;
                    }
                }
            });

            var colors = colorHistogram.OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .Take(colorCount)
                .Select(entry => entry.Key)
                .ToList();

            var palette = new ImagePalette(colors);
            return ImagePaletteResult.FromSuccess(palette, $"Extracted {colors.Count} colors from image palette");
        }
        catch (OperationCanceledException ex) {
            return ImagePaletteResult.FromException(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to get image palette with {ColorCount} colors", colorCount);
            return ImagePaletteResult.FromException(ex, GetPaletteFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ImageMetadata>> GetMetadataAsync(Stream imageStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageStream, nameof(imageStream));
        OperationHelpers.ThrowIfNotReadable(imageStream, $"Stream '{nameof(imageStream)}' must be readable.");
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.MetadataDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            var fileSize = GetStreamLength(imageStream);
            using var image = await Image.LoadAsync(imageStream, ct).ConfigureAwait(false);
            var format = DetectFormat(image.Metadata?.DecodedImageFormat);
            var (exifInfo, exifData) = ExifExtractor.Extract(image);
            var metadata = new ImageMetadata(
                image.Width, image.Height, format, fileSize, image.PixelType?.BitsPerPixel, image.PixelType?.AlphaRepresentation != null, exifInfo, exifData);

            return ImageMetadataResult.FromSuccess(metadata, $"Image metadata retrieved: {image.Width}x{image.Height}, Format: {format}");
        }
        catch (OperationCanceledException ex) {
            return ImageMetadataResult.FromException(ex, OperationCancelled);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to get image metadata");
            return ImageMetadataResult.FromException(ex, GetMetadataFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ImageMetadata>> GetMetadataFromFileAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        try {
            await using var stream = File.OpenRead(filePath);
            return await GetMetadataAsync(stream, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return ImageMetadataResult.FromException(ex, FileOperationFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CompressAsync(Stream inputStream, Stream outputStream, int quality, ImageFormat? format = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNotInRange(quality, 1, 100, nameof(quality));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.CompressDuration)]);
        ct.ThrowIfCancellationRequested();
        try {
            using var image = await Image.LoadAsync(inputStream, ct).ConfigureAwait(false);
            var encoder = GetEncoder(format ?? DefaultFormat, quality);
            await image.SaveAsync(outputStream, encoder, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<Result<bool>> ResizeFileAsync(
        string inputFilePath,
        string outputFilePath,
        int width,
        int height,
        ResizeMode resizeMode = ResizeMode.Max,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputFilePath, nameof(inputFilePath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
        try {
            await using var inputStream = File.OpenRead(inputFilePath);
            await using var outputStream = File.Create(outputFilePath);
            return await ResizeAsync(inputStream, outputStream, width, height, resizeMode, format, quality, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return ImageOperationResult.FromException(ex, "ResizeFile", FileOperationFailed);
        }
    }

    /// <inheritdoc />
    public async Task<BulkResult<ImageProcessRequest, ImageOperationResult>> ProcessBatchAsync(IEnumerable<ImageProcessRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests, nameof(requests));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BatchProcessDuration)]);
        var sw = Stopwatch.StartNew();
        var requestList = requests.ToList();
        var results = new List<Result<ImageProcessRequest, ImageOperationResult>>();
        foreach (var request in requestList) {
            ct.ThrowIfCancellationRequested();
            try {
                var operationResult = await ProcessRequestAsync(request, ct).ConfigureAwait(false);
                if (operationResult.IsSuccess)
                    results.Add(Result<ImageProcessRequest, ImageOperationResult>.Success(request, operationResult));
                else
                    results.Add(Result<ImageProcessRequest, ImageOperationResult>.Success(request, operationResult));
            }
            catch (OperationCanceledException ex) {
                var errorResult = ImageOperationResult.FromException(ex, "BatchProcess", OperationCancelled);
                results.Add(Result<ImageProcessRequest, ImageOperationResult>.Success(request, errorResult));
                break;
            }
            catch (Exception ex) {
                var errorResult = ImageOperationResult.FromException(ex, "BatchProcess");
                results.Add(Result<ImageProcessRequest, ImageOperationResult>.Success(request, errorResult));
            }
        }

        sw.Stop();
        return BulkResult<ImageProcessRequest, ImageOperationResult>.FromResults(results);
    }

    /// <summary>Gets the stream length when supported; returns null for non-seekable or unsupported streams.</summary>
    protected static long? GetStreamLength(Stream stream)
    {
        try {
            if (stream.CanSeek)
                return stream.Length;
        }
        catch (NotSupportedException) { }

        return null;
    }

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.ResizeDuration), Constants.Metrics.ResizeDuration },
            { nameof(Constants.Metrics.CropDuration), Constants.Metrics.CropDuration },
            { nameof(Constants.Metrics.RotateDuration), Constants.Metrics.RotateDuration },
            { nameof(Constants.Metrics.WatermarkDuration), Constants.Metrics.WatermarkDuration },
            { nameof(Constants.Metrics.ConvertDuration), Constants.Metrics.ConvertDuration },
            { nameof(Constants.Metrics.ThumbnailDuration), Constants.Metrics.ThumbnailDuration },
            { nameof(Constants.Metrics.MetadataDuration), Constants.Metrics.MetadataDuration },
            { nameof(Constants.Metrics.PaletteDuration), Constants.Metrics.PaletteDuration },
            { nameof(Constants.Metrics.CompressDuration), Constants.Metrics.CompressDuration },
            { nameof(Constants.Metrics.BatchProcessDuration), Constants.Metrics.BatchProcessDuration },
            { nameof(Constants.Metrics.CompositeOverlayDuration), Constants.Metrics.CompositeOverlayDuration }
        };

    /// <summary>Processes a single image request.</summary>
    protected virtual async Task<ImageOperationResult> ProcessRequestAsync(ImageProcessRequest request, CancellationToken ct)
    {
        Result<bool> result;
        var operationType = request.Operation.GetType().Name;
        switch (request.Operation) {
            case ResizeOperation resize:
                result = await ResizeAsync(request.InputStream, request.OutputStream, resize.Width, resize.Height, resize.Mode, request.TargetFormat, request.Quality, ct)
                    .ConfigureAwait(false);

                break;
            case CropOperation crop:
                result = await CropAsync(request.InputStream, request.OutputStream, crop.X, crop.Y, crop.Width, crop.Height, request.TargetFormat, request.Quality, ct)
                    .ConfigureAwait(false);

                break;
            case RotateOperation rotate:
                result = await RotateAsync(request.InputStream, request.OutputStream, rotate.Degrees, request.TargetFormat, request.Quality, ct).ConfigureAwait(false);
                break;
            case CompressOperation compress:
                result = await CompressAsync(request.InputStream, request.OutputStream, compress.Quality, request.TargetFormat, ct).ConfigureAwait(false);
                break;
            case ConvertFormatOperation convert:
                result = await ConvertFormatAsync(request.InputStream, request.OutputStream, convert.TargetFormat, request.Quality, ct).ConfigureAwait(false);
                break;
            default:
                var ex = new NotSupportedException($"Operation type {request.Operation.GetType().Name} is not supported");
                return ImageOperationResult.FromException(ex, operationType);
        }

        // Convert Result<bool> to ImageOperationResult
        if (result.IsSuccess)
            return ImageOperationResult.FromSuccess(operationType, result is ImageOperationResult imgOp ? imgOp.Message : null);

        var error = result.Errors?.FirstOrDefault();
        return error != null
            ? ImageOperationResult.FromError(error.Message, error.Code, operationType, error.Exception)
            : ImageOperationResult.FromError("Operation failed", "UNKNOWN", operationType);
    }

    /// <summary>Converts ResizeMode to ImageSharp ResizeMode.</summary>
    protected static ImageSharpResizeMode ConvertResizeMode(ResizeMode mode)
        => mode switch {
            ResizeMode.Max => ImageSharpResizeMode.Max,
            ResizeMode.Crop => ImageSharpResizeMode.Crop,
            ResizeMode.Pad => ImageSharpResizeMode.Pad,
            ResizeMode.BoxPad => ImageSharpResizeMode.BoxPad,
            ResizeMode.Stretch => ImageSharpResizeMode.Stretch,
            var _ => ImageSharpResizeMode.Max
        };

    /// <summary>Gets the appropriate encoder for the image format.</summary>
    protected static IImageEncoder GetEncoder(ImageFormat format, int? quality)
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

    /// <summary>Detects the image format from decoded format.</summary>
    protected static ImageFormat DetectFormat(IImageFormat? decodedFormat)
    {
        if (decodedFormat == null)
            return ImageFormat.Jpeg; // Default

        var formatName = decodedFormat.Name.ToLowerInvariant();
        return formatName switch {
            "jpeg" or "jpg" => ImageFormat.Jpeg,
            "png" => ImageFormat.Png,
            "gif" => ImageFormat.Gif,
            "bmp" => ImageFormat.Bmp,
            "webp" => ImageFormat.WebP,
            "tiff" or "tif" => ImageFormat.Tiff,
            "ico" => ImageFormat.Ico,
            var _ => ImageFormat.Jpeg // Default fallback
        };
    }

    /// <summary>Detects the image format from ImageInfo.</summary>
    protected static ImageFormat DetectFormat(ImageInfo info) => DetectFormat(info.Metadata?.DecodedImageFormat);
}