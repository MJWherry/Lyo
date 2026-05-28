using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.QRCode.Models;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QRCoder;
using static Lyo.QRCode.QRCodeErrorCodes;

namespace Lyo.QRCode.QRCoder;

/// <summary>
/// QR code service implementation using the QRCoder NuGet library. PNG/SVG are cross-platform; JPEG/BMP still require Windows because QRCoder routes them through
/// <c>System.Drawing</c>. Decoration (logo overlay, frame, caption, padding) is intentionally out of scope; chain <c>Lyo.Images.IImageDecorationService</c> on the returned bytes for
/// that.
/// </summary>
public class QRCoderQRCodeService : IQRCodeService
{
    private readonly ILogger<QRCoderQRCodeService> _logger;

    private readonly Dictionary<string, string> _metricNames;

    private readonly IMetrics _metrics;
    private readonly QRCodeServiceOptions _options;
    private readonly QRCodeGenerator _qrGenerator = new();

    /// <summary>Initializes a new instance of the <see cref="QRCoderQRCodeService" /> class.</summary>
    /// <param name="options">The QR code service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance.</param>
    public QRCoderQRCodeService(QRCodeServiceOptions options, ILogger<QRCoderQRCodeService>? logger = null, IMetrics? metrics = null)
    {
        _options = options;
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
        ArgumentHelpers.ThrowIfNull(builder);
        var (data, options) = builder.Build();
        return GenerateAsync(data, options, ct);
    }

    /// <summary>Generates a QR code from text/data.</summary>
    public async Task<Result<QRCodeRequest>> GenerateAsync(string data, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data);
        using var timer = _metrics.StartTimer(_metricNames[nameof(QRCode.Constants.Metrics.GenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var request = new QRCodeRequest { Data = data, Options = options };
        try {
            var qrOptions = options ??
                new QRCodeOptions { Format = _options.DefaultFormat, Size = _options.DefaultSize, ErrorCorrectionLevel = _options.DefaultErrorCorrectionLevel };

            ArgumentHelpers.ThrowIfNotInRange(qrOptions.Size, _options.MinSize, _options.MaxSize, nameof(options.Size));
            byte[] imageBytes;
            if (ShouldOffloadQrRasterization(data, qrOptions))
                imageBytes = await Task.Run(() => GenerateQRCodeBytes(data, qrOptions), ct).ConfigureAwait(false);
            else {
                ct.ThrowIfCancellationRequested();
                imageBytes = GenerateQRCodeBytes(data, qrOptions);
            }

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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data);
        ArgumentHelpers.ThrowIfNull(outputStream);
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        try {
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
        var successCount = results.Count(r => r is { IsSuccess: true, Data: { IsSuccess: true } });
        var failureCount = results.Count - successCount;
        _logger.LogDebug("Generated {Count} QR codes in batch: {SuccessCount} successful, {FailureCount} failed", requestList.Count, successCount, failureCount);
        return BulkResult<QRCodeRequest, QRCodeResult>.FromResults(results);
    }

    /// <inheritdoc />
    public Task<Result<QRCodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes);
        return Task.Run(() => QRCodeZxingRead.Decode(imageBytes), ct);
    }

    /// <summary>Heavy rasterization (large payloads or large pixels-per-module) runs on the thread pool; lighter work stays inline to avoid scheduling overhead.</summary>
    private static bool ShouldOffloadQrRasterization(string data, QRCodeOptions options) => data.Length > 4096 || options.Size > 512;

    /// <summary>Generates QR code bytes using QRCoder library.</summary>
    private byte[] GenerateQRCodeBytes(string data, QRCodeOptions options)
    {
        if (options.Format is QRCodeFormat.Jpeg or QRCodeFormat.Bitmap && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException($"QR code format '{options.Format}' requires Windows. Use PNG or SVG format on non-Windows platforms.");

        var eccLevel = ConvertErrorCorrectionLevel(QRCodeIconEccHelper.GetEffectiveLevel(options.ErrorCorrectionLevel, options.Icon));
        var qrCodeData = _qrGenerator.CreateQrCode(data, eccLevel);
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

    private static QRCodeGenerator.ECCLevel ConvertErrorCorrectionLevel(QRCodeErrorCorrectionLevel level)
        => level switch {
            QRCodeErrorCorrectionLevel.Low => QRCodeGenerator.ECCLevel.L,
            QRCodeErrorCorrectionLevel.Medium => QRCodeGenerator.ECCLevel.M,
            QRCodeErrorCorrectionLevel.Quartile => QRCodeGenerator.ECCLevel.Q,
            QRCodeErrorCorrectionLevel.High => QRCodeGenerator.ECCLevel.H,
            var _ => QRCodeGenerator.ECCLevel.M
        };

    private static byte[] GeneratePng(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(options.Size, ColorTranslator.FromHtml(options.DarkColor), ColorTranslator.FromHtml(options.LightColor), options.DrawQuietZones);
    }

    private static byte[] GenerateSvg(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new SvgQRCode(qrCodeData);
        var svgString = qrCode.GetGraphic(options.Size, ColorTranslator.FromHtml(options.DarkColor), ColorTranslator.FromHtml(options.LightColor), options.DrawQuietZones);
        return System.Text.Encoding.UTF8.GetBytes(svgString);
    }
#if OS_WINDOWS
    private static byte[] GenerateJpeg(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        var bitmapBytes = qrCode.GetGraphic(options.Size, options.DarkColor, options.LightColor);
        using var memoryStream = new MemoryStream(bitmapBytes);
        using var bitmap = new Bitmap(memoryStream);
        using var jpegStream = new MemoryStream();
        bitmap.Save(jpegStream, System.Drawing.Imaging.ImageFormat.Jpeg);
        return jpegStream.ToArray();
    }

    private static byte[] GenerateBitmap(QRCodeData qrCodeData, QRCodeOptions options)
    {
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        return qrCode.GetGraphic(options.Size, options.DarkColor, options.LightColor);
    }
#endif
}