using Lyo.Common.Core.Enums;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>
/// Fluent decoration chain. Stages queue lazily and run in order from <see cref="RunAsync" /> / <see cref="ToByteArrayAsync" />. One in-memory image is reused so steps do not
/// serialize between calls. Build via <see cref="IImageDecorationService.Pipeline(byte[])" />.
/// </summary>
public interface IImageDecorationPipeline
{
    /// <summary>Queues a centered or positioned overlay. <paramref name="overlayStream" /> is captured and read when the pipeline runs.</summary>
    IImageDecorationPipeline Overlay(Stream overlayStream, OverlayOptions options);

    /// <inheritdoc cref="Overlay(Stream, OverlayOptions)" />
    IImageDecorationPipeline Overlay(byte[] overlayBytes, OverlayOptions options);

    /// <summary>Queues a stroked frame (rounded outline plus optional fill) around the current image.</summary>
    IImageDecorationPipeline AddFrame(FrameOptions options);

    /// <summary>Queues a caption band (header or footer) around the current image.</summary>
    IImageDecorationPipeline AddCaption(CaptionOptions options);

    /// <summary>Queues an outer canvas margin or drop-shadow around the current image.</summary>
    IImageDecorationPipeline AddOuterPadding(PaddingOptions options);

    /// <summary>Runs queued stages and writes the encoded result to <paramref name="outputStream" />.</summary>
    Task<Result<bool>> RunAsync(Stream outputStream, ImageFormat? format = null, int? quality = null, CancellationToken ct = default);

    /// <summary>Runs queued stages and returns the encoded result as bytes.</summary>
    Task<Result<byte[]>> ToByteArrayAsync(ImageFormat? format = null, int? quality = null, CancellationToken ct = default);
}