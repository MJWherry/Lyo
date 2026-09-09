using System.Diagnostics;
using Lyo.Result;

namespace Lyo.QRCode.Models;

/// <summary>Outcome of a QR generation call, with QR-specific fields.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record QRCodeResult : Result<QRCodeRequest>
{
    /// <summary>Generated QR image bytes.</summary>
    public byte[]? ImageBytes { get; init; }

    /// <summary>Format used for the QR image.</summary>
    public QRCodeFormat? Format { get; init; }

    /// <summary>Pixels per module (same meaning as <see cref="QRCodeOptions.Size" />) when the implementation reports it.</summary>
    public int? Size { get; init; }

    /// <summary>Human-readable description of the result.</summary>
    public string? Message { get; init; }

    private QRCodeResult(bool isSuccess, QRCodeRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful result that holds generated image bytes.</summary>
    public static QRCodeResult FromSuccess(QRCodeRequest request, byte[] imageBytes, QRCodeFormat? format = null, int? size = null, string? message = null)
        => new(true, request) {
            ImageBytes = imageBytes,
            Format = format,
            Size = size,
            Message = message
        };

    /// <summary>Builds a failed result from an exception.</summary>
    public static QRCodeResult FromException(Exception exception, QRCodeRequest? request = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Builds a failed result with a custom error message.</summary>
    public static QRCodeResult FromError(string errorMessage, string errorCode, QRCodeRequest? request = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess
            ? $"Success: Format={Format}, Size={Size}, ImageBytes={ImageBytes?.Length ?? 0} bytes, Message={Message}, Timestamp={Timestamp:O}, Request={Data}"
            : $"Failure: {string.Join("; ", Errors ?? [])}, Timestamp={Timestamp:O}, Request={Data}";
}