using Lyo.Common;
using Lyo.Common.Enums;

namespace Lyo.Images.Models;

/// <summary>Result of a thumbnail generation operation with image-specific properties.</summary>
public sealed record ImageThumbnailResult : Result<byte[]>
{
    /// <summary>The maximum width of the thumbnail.</summary>
    public int? MaxWidth { get; init; }

    /// <summary>The maximum height of the thumbnail.</summary>
    public int? MaxHeight { get; init; }

    /// <summary>The image format used.</summary>
    public ImageFormat? Format { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private ImageThumbnailResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful ImageThumbnailResult with thumbnail bytes.</summary>
    public static ImageThumbnailResult FromSuccess(byte[] thumbnailBytes, int? maxWidth = null, int? maxHeight = null, ImageFormat? format = null, string? message = null)
        => new(true, thumbnailBytes) {
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
            Format = format,
            Message = message
        };

    /// <summary>Creates a failed ImageThumbnailResult from an exception.</summary>
    public static ImageThumbnailResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Creates a failed ImageThumbnailResult with a custom error message.</summary>
    public static ImageThumbnailResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }
}