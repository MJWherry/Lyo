using Lyo.Common.Enums;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>
/// Fluent chain of image-decoration primitives. Stages are queued lazily and applied in order by <see cref="RunAsync" /> / <see cref="ToByteArrayAsync" />; the pipeline keeps a
/// single in-memory image between stages so chained calls do not serialize between steps. Construct via <see cref="IImageDecorationService.Pipeline(byte[])" />.
/// </summary>
public interface IImageDecorationPipeline
{
    /// <summary>Queues a centered (or positioned) overlay stage. <paramref name="overlayStream" /> is captured and only read when the pipeline runs.</summary>
    IImageDecorationPipeline Overlay(Stream overlayStream, OverlayOptions options);

    /// <inheritdoc cref="Overlay(Stream, OverlayOptions)" />
    IImageDecorationPipeline Overlay(byte[] overlayBytes, OverlayOptions options);

    /// <summary>Queues a stroked-frame stage (rounded outline + optional fill) around the current image.</summary>
    IImageDecorationPipeline AddFrame(FrameOptions options);

    /// <summary>Queues a caption-band stage (header or footer) around the current image.</summary>
    IImageDecorationPipeline AddCaption(CaptionOptions options);

    /// <summary>Queues an outer canvas margin / drop-shadow stage around the current image.</summary>
    IImageDecorationPipeline AddOuterPadding(PaddingOptions options);

    /// <summary>Runs the queued stages and writes the encoded result to <paramref name="outputStream" />.</summary>
    Task<Result<bool>> RunAsync(Stream outputStream, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>Runs the queued stages and returns the encoded result as a byte array.</summary>
    Task<Result<byte[]>> ToByteArrayAsync(ImageFormat? format = null, int? quality = null, CancellationToken ct = default);
}
