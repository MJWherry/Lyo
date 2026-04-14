using Lyo.Common;

namespace Lyo.QRCode.Models;

/// <summary>Result of a QR code generation operation with QR code-specific properties.</summary>
public sealed record QRCodeResult : Result<QRCodeRequest>
{
    /// <summary>The generated QR code image bytes.</summary>
    public byte[]? ImageBytes { get; init; }

    /// <summary>The QR code format used.</summary>
    public QRCodeFormat? Format { get; init; }

    /// <summary>The size of the QR code in pixels.</summary>
    public int? Size { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private QRCodeResult(bool isSuccess, QRCodeRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful QRCodeResult with generated image bytes.</summary>
    public static QRCodeResult FromSuccess(QRCodeRequest request, byte[] imageBytes, QRCodeFormat? format = null, int? size = null, string? message = null)
        => new(true, request) {
            ImageBytes = imageBytes,
            Format = format,
            Size = size,
            Message = message
        };

    /// <summary>Creates a failed QRCodeResult from an exception.</summary>
    public static QRCodeResult FromException(Exception exception, QRCodeRequest? request = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed QRCodeResult with a custom error message.</summary>
    public static QRCodeResult FromError(string errorMessage, string errorCode, QRCodeRequest? request = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}