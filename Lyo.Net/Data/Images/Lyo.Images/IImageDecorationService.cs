using Lyo.Common.Core.Enums;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>
/// Decoration primitives (centered overlay, stroked frame, caption band, outer padding/shadow). Not QR-specific — any raster (PNG) or, for overlay, an SVG document. Chain
/// via <see cref="Pipeline(byte[])" /> / <see cref="Pipeline(Stream)" /> instead of passing intermediate streams.
/// </summary>
public interface IImageDecorationService
{
    /// <summary>
    /// Composites <paramref name="overlayStream" /> onto <paramref name="backgroundStream" /> at the position in <paramref name="options" />. When
    /// <paramref name="format" /> is <see cref="ImageFormat.Svg" />, the background is SVG text and the overlay is a base64 PNG image element. Otherwise ImageSharp runs on a raster.
    /// </summary>
    Task<Result<bool>> OverlayAsync(
        Stream backgroundStream,
        Stream overlayStream,
        Stream outputStream,
        OverlayOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Stroked outline around the image (optional rounded corners and inner fill). No caption, shadow, or outer margin.</summary>
    Task<Result<bool>> AddFrameAsync(
        Stream inputStream,
        Stream outputStream,
        FrameOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Caption text band above or below the image. Optional rounded outer corners and a downward notch (badge style).</summary>
    Task<Result<bool>> AddCaptionAsync(
        Stream inputStream,
        Stream outputStream,
        CaptionOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Outer canvas margin around the image, with optional rounded card fill and drop shadow.</summary>
    Task<Result<bool>> AddOuterPaddingAsync(
        Stream inputStream,
        Stream outputStream,
        PaddingOptions options,
        ImageFormat? format = null,
        int? quality = null,
        CancellationToken ct = default);

    /// <summary>Starts a decoration pipeline. One in-memory image is reused between stages so chained calls skip serialize/deserialize.</summary>
    /// <param name="input">PNG/JPEG/etc. bytes (or SVG document bytes) that seed the pipeline.</param>
    IImageDecorationPipeline Pipeline(byte[] input);

    /// <inheritdoc cref="Pipeline(byte[])" />
    /// <param name="input">Stream at the start of the encoded image (or SVG document) that seeds the pipeline.</param>
    IImageDecorationPipeline Pipeline(Stream input);
}