using Lyo.Exceptions;
using Lyo.Images.Models;
using Lyo.Result;

namespace Lyo.Images;

/// <summary>Applies <see cref="QrFrameLayoutCompositor" /> to square QR PNG bytes.</summary>
public sealed class QrFrameLayoutService : IQrFrameLayoutService
{
    private readonly ImageServiceOptions? _imageServiceOptions;

    /// <summary>Initializes compositor service. <paramref name="imageServiceOptions" /> is optional; when null, default PNG settings apply.</summary>
    public QrFrameLayoutService(ImageServiceOptions? imageServiceOptions = null) => _imageServiceOptions = imageServiceOptions;

    /// <inheritdoc />
    public async Task<Result<byte[]>> CompositeQrFramePngAsync(byte[] qrPng, QrFrameLayoutOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(qrPng);
        ArgumentHelpers.ThrowIfNull(options);
        ct.ThrowIfCancellationRequested();
        try {
            var useFast = _imageServiceOptions?.UseFastPngForQrComposites ?? false;
            var bytes = await QrFrameLayoutCompositor.ApplyAsync(qrPng, options, ct, useFast).ConfigureAwait(false);
            return Result<byte[]>.Success(bytes);
        }
        catch (OperationCanceledException ex) {
            return Result<byte[]>.Failure(ex, ImageErrorCodes.OperationCancelled);
        }
        catch (Exception ex) {
            return Result<byte[]>.Failure(ex, ImageErrorCodes.QrFrameCompositeFailed);
        }
    }
}