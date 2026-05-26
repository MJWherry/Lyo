using Lyo.Common.Enums;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>
/// Image-decoration primitives (centered overlay, stroked frame, caption band, outer padding/shadow). Implemented without QR-specific assumptions — usable for any raster (PNG)
/// or, in the overlay case, SVG document. Use <see cref="Pipeline(byte[])" /> / <see cref="Pipeline(Stream)" /> to chain primitives without juggling intermediate streams.
/// </summary>
public interface IImageDecorationService
{
    /// <summary>
    /// Composites <paramref name="overlayStream" /> onto <paramref name="backgroundStream" /> at the position requested by <paramref name="options" />. When
    /// <paramref name="format" /> is <see cref="ImageFormat.Svg" />, the background is treated as SVG text and the overlay is embedded as a base64 PNG image element. Otherwise
    /// the operation runs through ImageSharp on a raster.
    /// </summary>
    Task<Result<bool>> OverlayAsync(
        Stream backgroundStream,
        Stream overlayStream,
        Stream outputStream,
        OverlayOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Draws a stroked outline (with optional rounded corners and inner fill) around the input image. No caption, no shadow, no outer margin.</summary>
    Task<Result<bool>> AddFrameAsync(
        Stream inputStream,
        Stream outputStream,
        FrameOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Adds a caption text band above or below the input image. Optional rounded outside corners and a downward notch (badge-style).</summary>
    Task<Result<bool>> AddCaptionAsync(
        Stream inputStream,
        Stream outputStream,
        CaptionOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Adds outer canvas margin around the input image with an optional rounded card fill and drop shadow.</summary>
    Task<Result<bool>> AddOuterPaddingAsync(
        Stream inputStream,
        Stream outputStream,
        PaddingOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Starts a chained pipeline of decoration primitives. The pipeline keeps a single in-memory image between stages so chained calls do not serialize/deserialize each step.</summary>
    /// <param name="input">PNG/JPEG/etc. bytes (or SVG document bytes) to seed the pipeline.</param>
    IImageDecorationPipeline Pipeline(byte[] input);

    /// <inheritdoc cref="Pipeline(byte[])" />
    /// <param name="input">Stream positioned at the start of the encoded image (or SVG document) to seed the pipeline.</param>
    IImageDecorationPipeline Pipeline(Stream input);
}
