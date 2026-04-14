using Lyo.Common;
using Lyo.Images.Models;

namespace Lyo.Images;

/// <summary>
/// Composites decorative PNG frames around an existing square QR raster. Implemented without requiring the full <see cref="IImageService" /> surface area—register next to QR so frames work even when only QR + ImageSharp drawing are needed.
/// </summary>
public interface IQrFrameLayoutService
{
    /// <inheritdoc cref="IImageService.CompositeQrFramePngAsync" />
    Task<Result<byte[]>> CompositeQrFramePngAsync(byte[] qrPng, QrFrameLayoutOptions options, CancellationToken ct = default);
}
