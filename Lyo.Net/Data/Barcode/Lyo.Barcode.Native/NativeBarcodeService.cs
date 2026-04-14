using System.Diagnostics;
using Lyo.Barcode.Models;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Barcode.BarcodeErrorCodes;

namespace Lyo.Barcode.Native;

/// <summary>Barcode generation using an in-house Code 128 encoder and BMP/SVG rasterization (no third-party barcode packages).</summary>
public class NativeBarcodeService : IBarcodeService
{
    private readonly ILogger<NativeBarcodeService> _logger;
    private readonly Dictionary<string, string> _metricNames;
    private readonly IMetrics _metrics;
    private readonly BarcodeServiceOptions _options;

    public NativeBarcodeService(BarcodeServiceOptions options, ILogger<NativeBarcodeService>? logger = null, IMetrics? metrics = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<NativeBarcodeService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = new() {
            { nameof(Constants.Metrics.GenerateDuration), Constants.Metrics.GenerateDuration },
            { nameof(Constants.Metrics.BatchGenerateDuration), Constants.Metrics.BatchGenerateDuration },
            { nameof(Constants.Metrics.GenerateSuccess), Constants.Metrics.GenerateSuccess },
            { nameof(Constants.Metrics.GenerateFailure), Constants.Metrics.GenerateFailure },
            { nameof(Constants.Metrics.GenerateCancelled), Constants.Metrics.GenerateCancelled }
        };
    }

    public BarcodeFormat DefaultFormat => _options.DefaultFormat;

    public Task<Result<BarcodeRequest>> GenerateAsync(BarcodeBuilder builder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        var (data, symbology, options) = builder.Build();
        return GenerateAsync(data, symbology, options, ct);
    }

    public async Task<Result<BarcodeRequest>> GenerateAsync(string data, BarcodeSymbology symbology, BarcodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.GenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var request = new BarcodeRequest { Data = data, Symbology = symbology, Options = options };
        try {
            if (symbology != BarcodeSymbology.Code128)
                return BarcodeResult.FromError("Symbology is not supported by the native encoder.", UnsupportedSymbology, request);

            var o = options ?? new BarcodeOptions {
                Format = _options.DefaultFormat,
                ModuleWidthPixels = _options.DefaultModuleWidthPixels,
                BarHeightPixels = _options.DefaultBarHeightPixels,
                QuietZoneModules = _options.DefaultQuietZoneModules
            };

            ArgumentHelpers.ThrowIfNotInRange(o.ModuleWidthPixels, _options.MinModuleWidthPixels, _options.MaxModuleWidthPixels, nameof(options.ModuleWidthPixels));
            ArgumentHelpers.ThrowIfNotInRange(o.BarHeightPixels, _options.MinBarHeightPixels, _options.MaxBarHeightPixels, nameof(options.BarHeightPixels));
            ArgumentHelpers.ThrowIfNotInRange(o.QuietZoneModules, _options.MinQuietZoneModules, _options.MaxQuietZoneModules, nameof(options.QuietZoneModules));
            var (imageBytes, w, h) = await Task.Run(
                    () => {
                        var modules = Code128Encoder.EncodeCode128B(data);
                        var bytes = BarcodeImageRenderer.Render(modules, o);
                        var quiet = BarcodeImageRenderer.ResolveQuietZoneModules(o.QuietZoneModules);
                        var fullModules = modules.Length + 2 * quiet;
                        var widthPx = fullModules * o.ModuleWidthPixels;
                        var quietPx = quiet * o.ModuleWidthPixels;
                        var heightPx = o.BarHeightPixels + 2 * quietPx;
                        return (bytes, widthPx, heightPx);
                    }, ct)
                .ConfigureAwait(false);

            sw.Stop();
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateSuccess)]);
            _logger.LogDebug("Generated Code 128 barcode: {Length} chars, Format: {Format}, Size: {W}x{H}px", data.Length, o.Format, w, h);
            return BarcodeResult.FromSuccess(request, imageBytes, o.Format, w, h, $"Barcode generated. Format: {o.Format}, Size: {w}x{h}px");
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            _logger.LogWarning(ex, "Barcode generation was cancelled");
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateCancelled)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.GenerateDuration)], ex);
            return BarcodeResult.FromException(ex, request, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to generate barcode for data: {Data}", data);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.GenerateFailure)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.GenerateDuration)], ex);
            return BarcodeResult.FromException(ex, request, GenerateFailed);
        }
    }

    public async Task<Result<bool>> GenerateToStreamAsync(
        string data,
        BarcodeSymbology symbology,
        Stream outputStream,
        BarcodeOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        try {
            var result = await GenerateAsync(data, symbology, options, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Errors ?? []);

            if (result is BarcodeResult br && br.ImageBytes != null) {
                await outputStream.WriteAsync(br.ImageBytes, ct).ConfigureAwait(false);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(new Error("Barcode generation succeeded but image bytes are missing", GenerateFailed));
        }
        catch (Exception ex) {
            return Result<bool>.Failure(Error.FromException(ex, StreamOperationFailed));
        }
    }

    public async Task<Result<bool>> GenerateToFileAsync(string data, BarcodeSymbology symbology, string filePath, BarcodeOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        try {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await using var fileStream = File.Create(filePath);
            var result = await GenerateToStreamAsync(data, symbology, fileStream, options, ct).ConfigureAwait(false);
            if (result.IsSuccess)
                _logger.LogDebug("Saved barcode to file: {FilePath}", filePath);

            return result;
        }
        catch (Exception ex) {
            return Result<bool>.Failure(Error.FromException(ex, FileOperationFailed));
        }
    }

    public async Task<BulkResult<BarcodeRequest, BarcodeResult>> GenerateBatchAsync(IEnumerable<BarcodeRequest> requests, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.BatchGenerateDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        var requestList = requests.ToList();
        var results = new List<Result<BarcodeRequest, BarcodeResult>>();
        foreach (var request in requestList) {
            ct.ThrowIfCancellationRequested();
            var result = await GenerateAsync(request.Data, request.Symbology, request.Options, ct).ConfigureAwait(false);
            if (result is BarcodeResult br)
                results.Add(Result<BarcodeRequest, BarcodeResult>.Success(request, br));
            else {
                var errorResult = BarcodeResult.FromError("Invalid result type", GenerateFailed, request);
                results.Add(Result<BarcodeRequest, BarcodeResult>.Success(request, errorResult));
            }
        }

        sw.Stop();
        var successCount = results.Count(r => r.IsSuccess && r.Data is BarcodeResult { IsSuccess: true });
        var failureCount = results.Count - successCount;
        _logger.LogDebug("Generated {Count} barcodes in batch: {SuccessCount} successful, {FailureCount} failed", requestList.Count, successCount, failureCount);
        return BulkResult<BarcodeRequest, BarcodeResult>.FromResults(results);
    }

    /// <inheritdoc />
    public Task<Result<BarcodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes, nameof(imageBytes));
        return Task.Run(() => BarcodeZxingRead.Decode(imageBytes), ct);
    }
}