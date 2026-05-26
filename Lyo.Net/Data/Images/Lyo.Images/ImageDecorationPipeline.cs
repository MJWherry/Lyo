using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Images.Decoration;
using Lyo.Images.Models;
using Lyo.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Lyo.Images.ImageErrorCodes;

namespace Lyo.Images;

/// <summary>Default <see cref="IImageDecorationPipeline" /> implementation: holds a raster <see cref="Image{Rgba32}" /> or SVG text between stages and applies queued mutations in order.</summary>
internal sealed class ImageDecorationPipeline : IImageDecorationPipeline
{
    private readonly ImageServiceOptions _options;
    private readonly List<Func<PipelineState, CancellationToken, Task>> _stages = new();
    private readonly Func<CancellationToken, Task<PipelineState>> _stateFactory;

    public ImageDecorationPipeline(byte[] input, ImageServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(input);
        _options = options;
        _stateFactory = ct => PipelineState.LoadAsync(input, ct);
    }

    public ImageDecorationPipeline(Stream input, ImageServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(input);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        _options = options;
        _stateFactory = async ct => {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms, ct).ConfigureAwait(false);
            return await PipelineState.LoadAsync(ms.ToArray(), ct).ConfigureAwait(false);
        };
    }

    public IImageDecorationPipeline Overlay(Stream overlayStream, OverlayOptions options)
    {
        ArgumentHelpers.ThrowIfNull(overlayStream);
        ArgumentHelpers.ThrowIfNull(options);
        _stages.Add(async (state, ct) => {
            await using var ms = new MemoryStream();
            await overlayStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            await ApplyOverlayAsync(state, ms.ToArray(), options, ct).ConfigureAwait(false);
        });

        return this;
    }

    public IImageDecorationPipeline Overlay(byte[] overlayBytes, OverlayOptions options)
    {
        ArgumentHelpers.ThrowIfNull(overlayBytes);
        ArgumentHelpers.ThrowIfNull(options);
        _stages.Add((state, ct) => ApplyOverlayAsync(state, overlayBytes, options, ct));
        return this;
    }

    public IImageDecorationPipeline AddFrame(FrameOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _stages.Add((state, _) => {
            state.RequireRaster("AddFrame");
            var next = FrameDrawer.Apply(state.Raster!, options);
            state.ReplaceRaster(next);
            return Task.CompletedTask;
        });

        return this;
    }

    public IImageDecorationPipeline AddCaption(CaptionOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Text);
        _stages.Add((state, _) => {
            state.RequireRaster("AddCaption");
            var next = CaptionDrawer.Apply(state.Raster!, options);
            state.ReplaceRaster(next);
            return Task.CompletedTask;
        });

        return this;
    }

    public IImageDecorationPipeline AddOuterPadding(PaddingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _stages.Add((state, _) => {
            state.RequireRaster("AddOuterPadding");
            var next = OuterPaddingDrawer.Apply(state.Raster!, options);
            state.ReplaceRaster(next);
            return Task.CompletedTask;
        });

        return this;
    }

    public async Task<Result<bool>> RunAsync(Stream outputStream, ImageFormat? format = null, int? quality = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        try {
            using var state = await ExecuteAsync(ct).ConfigureAwait(false);
            await state.WriteAsync(outputStream, format, quality, _options, ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException ex) {
            return Result<bool>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            return Result<bool>.Failure(ex, CompositeOverlayFailed);
        }
    }

    public async Task<Result<byte[]>> ToByteArrayAsync(ImageFormat? format = null, int? quality = null, CancellationToken ct = default)
    {
        try {
            using var state = await ExecuteAsync(ct).ConfigureAwait(false);
            await using var ms = new MemoryStream();
            await state.WriteAsync(ms, format, quality, _options, ct).ConfigureAwait(false);
            return Result<byte[]>.Success(ms.ToArray());
        }
        catch (OperationCanceledException ex) {
            return Result<byte[]>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            return Result<byte[]>.Failure(ex, CompositeOverlayFailed);
        }
    }

    private async Task<PipelineState> ExecuteAsync(CancellationToken ct)
    {
        var state = await _stateFactory(ct).ConfigureAwait(false);
        try {
            foreach (var stage in _stages) {
                ct.ThrowIfCancellationRequested();
                await stage(state, ct).ConfigureAwait(false);
            }
        }
        catch {
            state.Dispose();
            throw;
        }

        return state;
    }

    private static async Task ApplyOverlayAsync(PipelineState state, byte[] overlayBytes, OverlayOptions options, CancellationToken ct)
    {
        if (state.IsSvg) {
            state.Svg = await OverlayDrawer.ApplyToSvgAsync(state.Svg!, overlayBytes, options, ct).ConfigureAwait(false);
            return;
        }

        await OverlayDrawer.ApplyToRasterAsync(state.Raster!, overlayBytes, options, ct).ConfigureAwait(false);
    }

    private sealed class PipelineState : IDisposable
    {
        public Image<Rgba32>? Raster { get; private set; }

        public string? Svg { get; set; }

        public bool IsSvg => Svg != null;

        public static async Task<PipelineState> LoadAsync(byte[] input, CancellationToken ct)
        {
            var state = new PipelineState();
            if (LooksLikeSvg(input))
                state.Svg = System.Text.Encoding.UTF8.GetString(input);
            else {
                await using var ms = new MemoryStream(input, false);
                state.Raster = await Image.LoadAsync<Rgba32>(ms, ct).ConfigureAwait(false);
            }

            return state;
        }

        public void ReplaceRaster(Image<Rgba32> next)
        {
            Raster?.Dispose();
            Raster = next;
        }

        public void RequireRaster(string operation)
        {
            if (Raster == null)
                throw new NotSupportedException($"{operation} requires a raster input; chain an Overlay (or pre-rasterize the SVG) before this stage.");
        }

        public async Task WriteAsync(Stream output, ImageFormat? format, int? quality, ImageServiceOptions options, CancellationToken ct)
        {
            if (Svg != null) {
                var bytes = System.Text.Encoding.UTF8.GetBytes(Svg);
                await output.WriteAsync(bytes.AsMemory(0, bytes.Length), ct).ConfigureAwait(false);
                return;
            }

            ArgumentHelpers.ThrowIfNull(Raster);
            var fmt = format ?? ImageFormat.Png;
            var encoder = fmt == ImageFormat.Png ? ImagePngEncoding.TruecolorForComposites(options.UseFastPngForQrComposites) : ImageEncoderFactory.GetEncoder(fmt, quality);
            await Raster.SaveAsync(output, encoder, ct).ConfigureAwait(false);
        }

        public void Dispose() => Raster?.Dispose();

        private static bool LooksLikeSvg(byte[] input)
        {
            // Quick sniff: skip BOM/whitespace; SVG documents begin with "<?xml" or "<svg".
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
}
