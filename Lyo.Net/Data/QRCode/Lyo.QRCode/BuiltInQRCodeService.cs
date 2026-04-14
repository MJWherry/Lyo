using System.Diagnostics;
using System.Linq;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Metrics;
using Lyo.QRCode.Encoding;
using Lyo.QRCode.Encoding.Iso;
using Lyo.QRCode.Models;
using Lyo.QRCode.QrGraphics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.QRCode.QRCodeErrorCodes;

namespace Lyo.QRCode;

/// <summary>
/// QR generation using the in-library ISO/IEC 18004 encoder (no QRCoder NuGet dependency). Supports PNG and SVG; JPEG and BMP require Windows (same constraint as the
/// QRCoder-backed service).
/// </summary>
public class BuiltInQRCodeService : IQRCodeService
{
    private readonly IImageService? _imageService;
    private readonly IQrFrameLayoutService? _qrFrameLayout;
    private readonly ILogger<BuiltInQRCodeService> _logger;
    private readonly Dictionary<string, string> _metricNames;
    private readonly IMetrics _metrics;
    private readonly QRCodeServiceOptions _options;

    public BuiltInQRCodeService(
        QRCodeServiceOptions options,
        ILogger<BuiltInQRCodeService>? logger = null,
        IMetrics? metrics = null,
        IImageService? imageService = null,
        IQrFrameLayoutService? qrFrameLayout = null)
    {
        _options = options;
        _imageService = imageService;
        _qrFrameLayout = qrFrameLayout;
        _logger = logger ?? NullLogger<BuiltInQRCodeService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = new() {
            { nameof(Constants.Metrics.GenerateDuration), Constants.Metrics.GenerateDuration },
            { nameof(Constants.Metrics.BatchGenerateDuration), Constants.Metrics.BatchGenerateDuration },
            { nameof(Constants.Metrics.GenerateSuccess), Constants.Metrics.GenerateSuccess },
            { nameof(Constants.Metrics.GenerateFailure), Constants.Metrics.GenerateFailure },
            { nameof(Constants.Metrics.GenerateCancelled), Constants.Metrics.GenerateCancelled }
        };
    }

    /// <inheritdoc />
    public QRCodeFormat DefaultFormat => _options.DefaultFormat;

    /// <inheritdoc />
    public Task<Result<QRCodeRequest>> GenerateAsync(QRCodeBuilder builder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        var (data, options) = builder.Build();
        return GenerateAsync(data, options, ct);
    }

    /// <inheritdoc />
    public async Task<Result<QRCodeRequest>> GenerateAsync(string data, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.GenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var request = new QRCodeRequest { Data = data, Options = options };
        try {
            var qrOptions = options ??
                new QRCodeOptions { Format = _options.DefaultFormat, Size = _options.DefaultSize, ErrorCorrectionLevel = _options.DefaultErrorCorrectionLevel };

            ArgumentHelpers.ThrowIfNotInRange(qrOptions.Size, _options.MinSize, _options.MaxSize, nameof(options.Size));
            var imageBytes = await GenerateQrCodeBytesAsync(data, qrOptions, ct).ConfigureAwait(false);
            sw.Stop();
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateSuccess)]);
            _logger.LogDebug("Generated QR code (built-in): {DataLength} bytes, Format: {Format}, Size: {Size}px", data.Length, qrOptions.Format, qrOptions.Size);
            return QRCodeResult.FromSuccess(
                request, imageBytes, qrOptions.Format, qrOptions.Size, $"QR code generated successfully. Format: {qrOptions.Format}, Size: {qrOptions.Size}px");
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            _logger.LogWarning(ex, "QR code generation was cancelled");
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateCancelled)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.GenerateDuration)], ex);
            return QRCodeResult.FromException(ex, request, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to generate QR code for data: {Data}", data);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateFailure)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.GenerateDuration)], ex);
            return QRCodeResult.FromException(ex, request, GenerateFailed);
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Result<bool>> GenerateToFileAsync(string data, string filePath, QRCodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
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

    /// <inheritdoc />
    public async Task<BulkResult<QRCodeRequest, QRCodeResult>> GenerateBatchAsync(IEnumerable<QRCodeRequest> requests, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.BatchGenerateDuration)]);
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

    private async Task<byte[]> GenerateQrCodeBytesAsync(string data, QRCodeOptions options, CancellationToken ct)
    {
        if (options.Format is QRCodeFormat.Jpeg or QRCodeFormat.Bitmap)
            throw new PlatformNotSupportedException("The built-in QR service supports PNG and SVG only. Use Lyo.QRCode.QRCoder for JPEG/BMP, or select PNG/SVG.");

        var ecc = ToEcc(QRCodeIconEccHelper.GetEffectiveLevel(options.ErrorCorrectionLevel, options.Icon));
        using var matrix = QrIsoEncoder.GenerateQrCode(data, ecc);
        var dark = QrHexColor.ToRgba(options.DarkColor);
        var light = QrHexColor.ToRgba(options.LightColor);
        var bytes = options.Format switch {
            QRCodeFormat.Png => QrIsoPngRasterizer.ToPng(matrix, options.Size, dark, light, options.DrawQuietZones),
            QRCodeFormat.Svg => System.Text.Encoding.UTF8.GetBytes(QrIsoSvgRasterizer.ToSvg(matrix, options.Size, options.DarkColor, options.LightColor, options.DrawQuietZones)),
            var _ => QrIsoPngRasterizer.ToPng(matrix, options.Size, dark, light, options.DrawQuietZones)
        };

        if (options.Icon != null && QrCodeIconComposer.TryResolveIconBytes(options.Icon) != null) {
            if (_imageService == null) {
                throw new InvalidOperationException(
                    "Embedding a QR icon requires IImageService. Register an image service (for example AddImageSharpImageService) and pass IImageService into BuiltInQRCodeService, or remove the icon.");
            }

            if (options.Format == QRCodeFormat.Png)
                bytes = await QrCodeIconComposer.ApplyIconToPngAsync(_imageService, bytes, options.Icon, options.LightColor, _logger, ct).ConfigureAwait(false);
            else if (options.Format == QRCodeFormat.Svg) {
                var svg = System.Text.Encoding.UTF8.GetString(bytes);
                var withIcon = await QrCodeIconComposer.ApplyIconToSvgAsync(_imageService, svg, options.Icon, options.Size, options.LightColor, _logger, ct).ConfigureAwait(false);
                bytes = System.Text.Encoding.UTF8.GetBytes(withIcon);
            }
        }

        if (options.Format == QRCodeFormat.Png && options.Frame is { Style: not QrFrameStyle.None }) {
            Result<byte[]> framed;
            if (_qrFrameLayout != null)
                framed = await _qrFrameLayout.CompositeQrFramePngAsync(bytes, options.Frame, ct).ConfigureAwait(false);
            else if (_imageService != null)
                framed = await _imageService.CompositeQrFramePngAsync(bytes, options.Frame, ct).ConfigureAwait(false);
            else {
                throw new InvalidOperationException(
                    "Decorative QR frames require IQrFrameLayoutService (registered with AddQRCodeService) or IImageService, or set Frame to none.");
            }
            if (!framed.IsSuccess || framed.Data == null) {
                var msg = framed.Errors is { Count: > 0 } ? string.Join("; ", framed.Errors.Select(e => e.Message)) : "Unknown error";
                throw new InvalidOperationException($"Failed to apply QR frame: {msg}");
            }

            bytes = framed.Data;
        }

        return bytes;
    }

    private static QrIsoEncoder.ECCLevel ToEcc(QRCodeErrorCorrectionLevel level)
        => level switch {
            QRCodeErrorCorrectionLevel.Low => QrIsoEncoder.ECCLevel.L,
            QRCodeErrorCorrectionLevel.Medium => QrIsoEncoder.ECCLevel.M,
            QRCodeErrorCorrectionLevel.Quartile => QrIsoEncoder.ECCLevel.Q,
            QRCodeErrorCorrectionLevel.High => QrIsoEncoder.ECCLevel.H,
            var _ => QrIsoEncoder.ECCLevel.M
        };
}