using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Images.Decoration;
using Lyo.Images.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Lyo.Images.ImageErrorCodes;

namespace Lyo.Images;

/// <summary>
/// Default <see cref="IImageDecorationService" /> implementation: stream-based overlay/frame/caption/padding primitives plus the
/// <see cref="ImageDecorationPipeline" /> factory. The compositors themselves live in <c>Lyo.Images.Decoration</c>.
/// </summary>
public class ImageDecorationService : IImageDecorationService
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly ImageServiceOptions _options;

    /// <summary>Initializes a new <see cref="ImageDecorationService" />.</summary>
    /// <param name="options">Image service options; falls back to defaults when null.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics sink (only used when <see cref="ImageServiceOptions.EnableMetrics" /> is true).</param>
    public ImageDecorationService(ImageServiceOptions? options = null, ILogger<ImageDecorationService>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new ImageServiceOptions();
        _logger = (ILogger?)logger ?? NullLogger.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> OverlayAsync(
        Stream backgroundStream,
        Stream overlayStream,
        Stream outputStream,
        OverlayOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ValidateStreams(backgroundStream, outputStream);
        ArgumentHelpers.ThrowIfNull(overlayStream);
        OperationHelpers.ThrowIfNotReadable(overlayStream, $"Stream '{nameof(overlayStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNull(options);
        using var timer = _metrics.StartTimer(Constants.Metrics.CompositeOverlayDuration);
        try {
            await using var bg = new MemoryStream();
            await backgroundStream.CopyToAsync(bg, ct).ConfigureAwait(false);
            await using var ov = new MemoryStream();
            await overlayStream.CopyToAsync(ov, ct).ConfigureAwait(false);
            var bgBytes = bg.ToArray();
            var overlayBytes = ov.ToArray();
            if (format == ImageFormat.Svg || LooksLikeSvg(bgBytes)) {
                var svg = System.Text.Encoding.UTF8.GetString(bgBytes);
                var modified = await OverlayDrawer.ApplyToSvgAsync(svg, overlayBytes, options, ct).ConfigureAwait(false);
                var bytes = System.Text.Encoding.UTF8.GetBytes(modified);
                await outputStream.WriteAsync(bytes.AsMemory(0, bytes.Length), ct).ConfigureAwait(false);
                return Result<bool>.Success(true);
            }

            await using var bgRead = new MemoryStream(bgBytes, false);
            using var image = await Image.LoadAsync<Rgba32>(bgRead, ct).ConfigureAwait(false);
            await OverlayDrawer.ApplyToRasterAsync(image, overlayBytes, options, ct).ConfigureAwait(false);
            await SaveAsync(image, outputStream, format, quality, ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException ex) {
            _logger.LogWarning(ex, "Overlay operation was cancelled");
            return Result<bool>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to apply overlay");
            return Result<bool>.Failure(ex, CompositeOverlayFailed);
        }
    }

    /// <inheritdoc />
    public Task<Result<bool>> AddFrameAsync(
        Stream inputStream,
        Stream outputStream,
        FrameOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
        => RunRasterStageAsync(inputStream, outputStream, options, format, quality, FrameDrawer.Apply, "Frame", FrameCompositeFailed, ct);

    /// <inheritdoc />
    public Task<Result<bool>> AddCaptionAsync(
        Stream inputStream,
        Stream outputStream,
        CaptionOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Text);
        return RunRasterStageAsync(inputStream, outputStream, options, format, quality, CaptionDrawer.Apply, "Caption", CaptionCompositeFailed, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> AddOuterPaddingAsync(
        Stream inputStream,
        Stream outputStream,
        PaddingOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default)
        => RunRasterStageAsync(inputStream, outputStream, options, format, quality, OuterPaddingDrawer.Apply, "OuterPadding", OuterPaddingCompositeFailed, ct);

    /// <inheritdoc />
    public IImageDecorationPipeline Pipeline(byte[] input) => new ImageDecorationPipeline(input, _options);

    /// <inheritdoc />
    public IImageDecorationPipeline Pipeline(Stream input) => new ImageDecorationPipeline(input, _options);

    private async Task<Result<bool>> RunRasterStageAsync<TOptions>(
        Stream inputStream,
        Stream outputStream,
        TOptions options,
        ImageFormat? format,
        int? quality,
        Func<Image<Rgba32>, TOptions, Image<Rgba32>> drawer,
        string operation,
        string errorCode,
        CancellationToken ct)
    {
        ValidateStreams(inputStream, outputStream);
        ArgumentHelpers.ThrowIfNull(options);
        try {
            using var input = await Image.LoadAsync<Rgba32>(inputStream, ct).ConfigureAwait(false);
            using var output = drawer(input, options);
            await SaveAsync(output, outputStream, format, quality, ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException ex) {
            _logger.LogWarning(ex, "{Operation} operation was cancelled", operation);
            return Result<bool>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to apply {Operation}", operation);
            return Result<bool>.Failure(ex, errorCode);
        }
    }

    private async Task SaveAsync(Image<Rgba32> image, Stream output, ImageFormat? format, int? quality, CancellationToken ct)
    {
        var fmt = format ?? ImageFormat.Png;
        var encoder = fmt == ImageFormat.Png ? ImagePngEncoding.TruecolorForComposites(_options.UseFastPngForQrComposites) : ImageEncoderFactory.GetEncoder(fmt, quality);
        await image.SaveAsync(output, encoder, ct).ConfigureAwait(false);
    }

    private static void ValidateStreams(Stream input, Stream output)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
    }

    private static bool LooksLikeSvg(byte[] input)
    {
        var i = 0;
        if (input.Length >= 3 && input[0] == 0xEF && input[1] == 0xBB && input[2] == 0xBF)
            i = 3;

        while (i < input.Length && (input[i] == ' ' || input[i] == '\t' || input[i] == '\r' || input[i] == '\n'))
            i++;

        if (i >= input.Length || input[i] != (byte)'<')
            return false;

        var head = System.Text.Encoding.ASCII.GetString(input, i, Math.Min(256, input.Length - i));
        return head.StartsWith("<?xml", StringComparison.Ordinal) && head.Contains("<svg", StringComparison.OrdinalIgnoreCase)
            || head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
