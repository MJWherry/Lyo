using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Barcode.Models;

/// <summary>Result of a barcode generation operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BarcodeResult : Result<BarcodeRequest>
{
    /// <summary>Generated image bytes (BMP or SVG markup bytes).</summary>
    public byte[]? ImageBytes { get; init; }

    /// <summary>Output format used for <see cref="ImageBytes" />.</summary>
    public BarcodeFormat? Format { get; init; }

    /// <summary>Raster width in pixels when known.</summary>
    public int? ImageWidthPixels { get; init; }

    /// <summary>Raster height in pixels when known.</summary>
    public int? ImageHeightPixels { get; init; }

    /// <summary>Optional human-readable status from the implementation.</summary>
    public string? Message { get; init; }

    private BarcodeResult(bool isSuccess, BarcodeRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful result with image payload.</summary>
    public static BarcodeResult FromSuccess(BarcodeRequest request, byte[] imageBytes, BarcodeFormat? format = null, int? width = null, int? height = null, string? message = null)
        => new(true, request) {
            ImageBytes = imageBytes,
            Format = format,
            ImageWidthPixels = width,
            ImageHeightPixels = height,
            Message = message
        };

    /// <summary>Creates a failed result from an exception.</summary>
    public static BarcodeResult FromException(Exception exception, BarcodeRequest? request = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed result from a message or optional exception.</summary>
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