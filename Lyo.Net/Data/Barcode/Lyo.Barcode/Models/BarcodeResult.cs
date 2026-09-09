using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Barcode.Models;

/// <summary>Outcome of one barcode render, including image bytes when it succeeds.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BarcodeResult : Result<BarcodeRequest>
{
    /// <summary>Rendered image as BMP bytes or SVG markup bytes.</summary>
    public byte[]? ImageBytes { get; init; }

    /// <summary>Image type stored in <see cref="ImageBytes" />.</summary>
    public BarcodeFormat? Format { get; init; }

    /// <summary>Pixel width of the raster when the implementation reports it.</summary>
    public int? ImageWidthPixels { get; init; }

    /// <summary>Pixel height of the raster when the implementation reports it.</summary>
    public int? ImageHeightPixels { get; init; }

    /// <summary>Optional status text supplied by the implementation.</summary>
    public string? Message { get; init; }

    private BarcodeResult(bool isSuccess, BarcodeRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a success result that carries the rendered image.</summary>
    public static BarcodeResult FromSuccess(BarcodeRequest request, byte[] imageBytes, BarcodeFormat? format = null, int? width = null, int? height = null, string? message = null)
        => new(true, request) {
            ImageBytes = imageBytes,
            Format = format,
            ImageWidthPixels = width,
            ImageHeightPixels = height,
            Message = message
        };

    /// <summary>Builds a failure result from an exception.</summary>
    public static BarcodeResult FromException(Exception exception, BarcodeRequest? request = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Builds a failure result from a message and optional exception.</summary>
    public static BarcodeResult FromError(string errorMessage, string errorCode, BarcodeRequest? request = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess
            ? $"Success: Format={Format}, {ImageWidthPixels}x{ImageHeightPixels} px, ImageBytes={ImageBytes?.Length ?? 0} bytes, Message={Message}, Timestamp={Timestamp:O}, Request={Data}"
            : $"Failure: {string.Join("; ", Errors ?? [])}, Timestamp={Timestamp:O}, Request={Data}";
}