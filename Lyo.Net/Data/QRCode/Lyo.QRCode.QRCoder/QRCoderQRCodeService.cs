using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Metrics;
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QRCoder;
using static Lyo.QRCode.QRCodeErrorCodes;
#if OS_WINDOWS
using System.Drawing.Imaging;
#endif

namespace Lyo.QRCode.QRCoder;

/// <summary>QR code service implementation using QRCoder library.</summary>
public class QRCoderQRCodeService : IQRCodeService
{
    private readonly ILogger<QRCoderQRCodeService> _logger;

    /// <summary>Gets the metric names dictionary.</summary>
    private readonly Dictionary<string, string> _metricNames;

    private readonly IMetrics _metrics;
    private readonly QRCodeServiceOptions _options;
    private readonly IImageService? _imageService;
    private readonly IQrFrameLayoutService? _qrFrameLayout;

    /// <summary>Initializes a new instance of the <see cref="QRCoderQRCodeService" /> class.</summary>
    /// <param name="options">The QR code service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance.</param>
    /// <param name="imageService">Optional image service for decorative PNG frames (and future cross-platform icon support).</param>
    /// <param name="qrFrameLayout">Optional frame compositor; preferred over <paramref name="imageService" /> for frames when both are registered.</param>
    public QRCoderQRCodeService(
        QRCodeServiceOptions options,
        ILogger<QRCoderQRCodeService>? logger = null,
        IMetrics? metrics = null,
        IImageService? imageService = null,
        IQrFrameLayoutService? qrFrameLayout = null)
    {
        _options = options;
        _imageService = imageService;
        _qrFrameLayout = qrFrameLayout;
        _logger = logger ?? NullLogger<QRCoderQRCodeService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = new() {
            { nameof(QRCode.Constants.Metrics.GenerateDuration), QRCode.Constants.Metrics.GenerateDuration },
            { nameof(QRCode.Constants.Metrics.BatchGenerateDuration), QRCode.Constants.Metrics.BatchGenerateDuration },
            { nameof(QRCode.Constants.Metrics.GenerateSuccess), QRCode.Constants.Metrics.GenerateSuccess },
            { nameof(QRCode.Constants.Metrics.GenerateFailure), QRCode.Constants.Metrics.GenerateFailure },
            { nameof(QRCode.Constants.Metrics.GenerateCancelled), QRCode.Constants.Metrics.GenerateCancelled }
        };
    }

    /// <summary>Gets the default QR code format.</summary>
    public QRCodeFormat DefaultFormat => _options.DefaultFormat;

    /// <summary>Generates a QR code using a builder.</summary>
    public Task<Result<QRCodeRequest>> GenerateAsync(QRCodeBuilder builder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        var (data, options) = builder.Build();
        return GenerateAsync(data, options, ct);
    }

    /// <summary>Generates a QR code from text/data.</summary>
    public async Task<Result<QRCodeRequest>> GenerateAsync(string data, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        using var timer = _metrics.StartTimer(_metricNames[nameof(QRCode.Constants.Metrics.GenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var request = new QRCodeRequest { Data = data, Options = options };
        try {
            var qrOptions = options ??
                new QRCodeOptions { Format = _options.DefaultFormat, Size = _options.DefaultSize, ErrorCorrectionLevel = _options.DefaultErrorCorrectionLevel };

            // Validate size
            ArgumentHelpers.ThrowIfNotInRange(qrOptions.Size, _options.MinSize, _options.MaxSize, nameof(options.Size));
            var imageBytes = await Task.Run(() => GenerateQRCodeBytes(data, qrOptions), ct).ConfigureAwait(false);
            imageBytes = await ApplyQrFrameIfNeededAsync(imageBytes, qrOptions, ct).ConfigureAwait(false);
            sw.Stop();
            _metrics.IncrementCounter(_metricNames[nameof(QRCode.Constants.Metrics.GenerateSuccess)]);
            _logger.LogDebug("Generated QR code: {DataLength} bytes, Format: {Format}, Size: {Size}px", data.Length, qrOptions.Format, qrOptions.Size);
            return QRCodeResult.FromSuccess(
                request, imageBytes, qrOptions.Format, qrOptions.Size, $"QR code generated successfully. Format: {qrOptions.Format}, Size: {qrOptions.Size}px");
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            _logger.LogWarning(ex, "QR code generation was cancelled");
            _metrics.IncrementCounter(_metricNames[nameof(QRCode.Constants.Metrics.GenerateCancelled)]);
            _metrics.RecordError(_metricNames[nameof(QRCode.Constants.Metrics.GenerateDuration)], ex);
            return QRCodeResult.FromException(ex, request, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to generate QR code for data: {Data}", data);
            _metrics.IncrementCounter(_metricNames[nameof(QRCode.Constants.Metrics.GenerateFailure)]);
            _metrics.RecordError(_metricNames[nameof(QRCode.Constants.Metrics.GenerateDuration)], ex);
            return QRCodeResult.FromException(ex, request, GenerateFailed);
        }
    }

    /// <summary>Generates a QR code and writes it to a stream.</summary>
    public async Task<Result<bool>> GenerateToStreamAsync(string data, Stream outputStream, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        try {
            var result = await GenerateAsync(data, options, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Errors ?? []);

            if (result is QRCodeResult qrResult && qrResult.ImageBytes != null) {
                await outputStream.WriteAsync(qrResult.ImageBytes, 0, qrResult.ImageBytes.Length, ct).ConfigureAwait(false);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(new Error("QR code generation succeeded but image bytes are missing", GenerateFailed));
        }
        catch (Exception ex) {
            return Result<bool>.Failure(Error.FromException(ex, StreamOperationFailed));
        }
    }

    /// <summary>Generates a QR code and saves it to a file.</summary>
    public async Task<Result<bool>> GenerateToFileAsync(string data, string filePath, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        try {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await using var fileStream = File.Create(filePath);
            var result = await GenerateToStreamAsync(data, fileStream, options, ct).ConfigureAwait(false);
            if (result.IsSuccess)
                _logger.LogDebug("Saved QR code to file: {FilePath}", filePath);

            return result;
        }
        catch (Exception ex) {
            return Result<bool>.Failure(Error.FromException(ex, FileOperationFailed));
        }
    }

    /// <summary>Generates multiple QR codes in batch.</summary>
    public async Task<BulkResult<QRCodeRequest, QRCodeResult>> GenerateBatchAsync(IEnumerable<QRCodeRequest> requests, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(_metricNames[nameof(QRCode.Constants.Metrics.BatchGenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var requestList = requests.ToList();
        var results = new List<Result<QRCodeRequest, QRCodeResult>>();
        foreach (var request in requestList) {
            ct.ThrowIfCancellationRequested();
            var result = await GenerateAsync(request.Data, request.Options, ct).ConfigureAwait(false);
            if (result is QRCodeResult qrResult)
                results.Add(Result<QRCodeRequest, QRCodeResult>.Success(request, qrResult));
            else {
                var errorResult = QRCodeResult.FromError("Invalid result type", GenerateFailed, request);
                results.Add(Result<QRCodeRequest, QRCodeResult>.Success(request, errorResult));
            }
        }

        sw.Stop();
        var successCount = results.Count(r => r.IsSuccess && r.Data is QRCodeResult { IsSuccess: true });
        var failureCount = results.Count - successCount;
        _logger.LogDebug("Generated {Count} QR codes in batch: {SuccessCount} successful, {FailureCount} failed", requestList.Count, successCount, failureCount);
        return BulkResult<QRCodeRequest, QRCodeResult>.FromResults(results);
    }

    /// <inheritdoc />
    public Task<Result<QRCodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes, nameof(imageBytes));
        return Task.Run(() => QRCodeZxingRead.Decode(imageBytes), ct);
    }

    private async Task<byte[]> ApplyQrFrameIfNeededAsync(byte[] imageBytes, QRCodeOptions options, CancellationToken ct)
    {
        if (options.Format != QRCodeFormat.Png || options.Frame is null or { Style: QrFrameStyle.None })
            return imageBytes;

        Task<Result<byte[]>> apply;
        if (_qrFrameLayout != null)
            apply = _qrFrameLayout.CompositeQrFramePngAsync(imageBytes, options.Frame, ct);
        else if (_imageService != null)
            apply = _imageService.CompositeQrFramePngAsync(imageBytes, options.Frame, ct);
        else {
            throw new InvalidOperationException(
                "Decorative QR frames require IQrFrameLayoutService (AddQRCoderQrCodeService registers it) or IImageService when using Frame, or set Frame.Style to None.");
        }

        var result = await apply.ConfigureAwait(false);
        if (!result.IsSuccess || result.Data == null) {
            var msg = result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors.Select(e => e.Message)) : "Unknown error";
            throw new InvalidOperationException($"Failed to apply QR frame: {msg}");
        }

        return result.Data;
    }

    /// <summary>Generates QR code bytes using QRCoder library.</summary>
    private byte[] GenerateQRCodeBytes(string data, QRCodeOptions options)
    {
        if (options.Format is QRCodeFormat.Jpeg or QRCodeFormat.Bitmap && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException($"QR code format '{options.Format}' requires Windows. Use PNG or SVG format on non-Windows platforms.");

        var eccLevel = ConvertErrorCorrectionLevel(QRCodeIconEccHelper.GetEffectiveLevel(options.ErrorCorrectionLevel, options.Icon));
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(data, eccLevel);
        return options.Format switch {
            QRCodeFormat.Png => GeneratePng(qrCodeData, options),
            QRCodeFormat.Svg => GenerateSvg(qrCodeData, options),
#if OS_WINDOWS
            QRCodeFormat.Jpeg => GenerateJpeg(qrCodeData, options),
            QRCodeFormat.Bitmap => GenerateBitmap(qrCodeData, options),
#endif
            var _ => GeneratePng(qrCodeData, options)
        };
    }

    /// <summary>Converts QRCodeErrorCorrectionLevel to QRCoder ECCLevel.</summary>
    private static QRCodeGenerator.ECCLevel ConvertErrorCorrectionLevel(QRCodeErrorCorrectionLevel level)
        => level switch {
            QRCodeErrorCorrectionLevel.Low => QRCodeGenerator.ECCLevel.L,
            QRCodeErrorCorrectionLevel.Medium => QRCodeGenerator.ECCLevel.M,
            QRCodeErrorCorrectionLevel.Quartile => QRCodeGenerator.ECCLevel.Q,
            QRCodeErrorCorrectionLevel.High => QRCodeGenerator.ECCLevel.H,
            var _ => QRCodeGenerator.ECCLevel.M
        };

    /// <summary>Generates PNG format QR code.</summary>
    private byte[] GeneratePng(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(options.Size, ColorTranslator.FromHtml(options.DarkColor), ColorTranslator.FromHtml(options.LightColor), options.DrawQuietZones);
#if OS_WINDOWS
        if (options.Icon != null)
            qrCodeBytes = ApplyIcon(qrCodeBytes, options);
#endif
        return qrCodeBytes;
    }

    /// <summary>Generates SVG format QR code.</summary>
    private byte[] GenerateSvg(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new SvgQRCode(qrCodeData);
        var svgString = qrCode.GetGraphic(options.Size, ColorTranslator.FromHtml(options.DarkColor), ColorTranslator.FromHtml(options.LightColor), options.DrawQuietZones);
#if OS_WINDOWS
        // Apply icon if specified (for SVG, we'd need to embed it)
        if (options.Icon != null)
            _logger.LogWarning("Icon embedding is not supported for SVG format");
#endif
        return System.Text.Encoding.UTF8.GetBytes(svgString);
    }
#if OS_WINDOWS
    /// <summary>Generates JPEG format QR code.</summary>
    private byte[] GenerateJpeg(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        var bitmapBytes = qrCode.GetGraphic(options.Size, options.DarkColor, options.LightColor);

        // Apply icon if specified
        if (options.Icon != null)
            bitmapBytes = ApplyIcon(bitmapBytes, options);

        // Convert bitmap bytes to JPEG
        using var bitmap = ByteArrayToBitmap(bitmapBytes, options.Size);
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Jpeg);
        return memoryStream.ToArray();
    }

    /// <summary>Generates Bitmap format QR code.</summary>
    private byte[] GenerateBitmap(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        var bitmapBytes = qrCode.GetGraphic(options.Size, options.DarkColor, options.LightColor);

        // Apply icon if specified
        if (options.Icon != null)
            bitmapBytes = ApplyIcon(bitmapBytes, options);

        return bitmapBytes;
    }

    /// <summary>Applies an icon to the center of the QR code.</summary>
    private byte[] ApplyIcon(byte[] qrCodeBytes, QRCodeOptions options)
    {
        if (options.Icon == null)
            return qrCodeBytes;

        var iconRaw = QrCodeIconComposer.TryResolveIconBytes(options.Icon);
        if (iconRaw == null || iconRaw.Length == 0)
            return qrCodeBytes;

        try {
            using var qrBitmap = ByteArrayToBitmap(qrCodeBytes);
            using var iconBitmap = ByteArrayToBitmap(iconRaw);
            var side = Math.Min(qrBitmap.Width, qrBitmap.Height);
            var pct = QRCodeIconOptions.ClampIconSizePercent(options.Icon.IconSizePercent);
            var iconSize = Math.Max(1, (int)(side * (pct / 100.0)));
            var iconX = (qrBitmap.Width - iconSize) / 2;
            var iconY = (qrBitmap.Height - iconSize) / 2;
            using var resizedIcon = new Bitmap(iconBitmap, iconSize, iconSize);
            using var graphics = Graphics.FromImage(qrBitmap);
            graphics.DrawImage(resizedIcon, iconX, iconY);
            if (options.Icon.DrawIconBorder) {
                using var pen = new Pen(ColorTranslator.FromHtml(options.LightColor), 2);
                graphics.DrawRectangle(pen, iconX - 1, iconY - 1, iconSize + 2, iconSize + 2);
            }

            using var resultStream = new MemoryStream();
            qrBitmap.Save(resultStream, ImageFormat.Png);
            return resultStream.ToArray();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to apply icon to QR code, returning QR code without icon");
            return qrCodeBytes;
        }
    }

    /// <summary>Converts byte array to Bitmap.</summary>
    private static Bitmap ByteArrayToBitmap(byte[] bytes, int? expectedSize = null)
    {
        using var memoryStream = new MemoryStream(bytes);
        var bitmap = new Bitmap(memoryStream);
        if (expectedSize.HasValue && (bitmap.Width != expectedSize.Value || bitmap.Height != expectedSize.Value)) {
            var resized = new Bitmap(bitmap, expectedSize.Value, expectedSize.Value);
            bitmap.Dispose();
            return resized;
        }

        return new(bitmap);
    }
#endif
}