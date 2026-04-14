using Lyo.Common;

namespace Lyo.Barcode.Models;

/// <summary>Result of a barcode generation operation.</summary>
public sealed record BarcodeResult : Result<BarcodeRequest>
{
    public byte[]? ImageBytes { get; init; }

    public BarcodeFormat? Format { get; init; }

    public int? ImageWidthPixels { get; init; }

    public int? ImageHeightPixels { get; init; }

    public string? Message { get; init; }

    private BarcodeResult(bool isSuccess, BarcodeRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    public static BarcodeResult FromSuccess(BarcodeRequest request, byte[] imageBytes, BarcodeFormat? format = null, int? width = null, int? height = null, string? message = null)
        => new(true, request) {
            ImageBytes = imageBytes,
            Format = format,
            ImageWidthPixels = width,
            ImageHeightPixels = height,
            Message = message
        };

    public static BarcodeResult FromException(Exception exception, BarcodeRequest? request = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    public static BarcodeResult FromError(string errorMessage, string errorCode, BarcodeRequest? request = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}