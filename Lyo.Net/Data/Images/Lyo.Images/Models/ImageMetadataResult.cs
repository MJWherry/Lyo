using Lyo.Common;

namespace Lyo.Images.Models;

/// <summary>Result of an image metadata retrieval operation with image-specific properties.</summary>
public sealed record ImageMetadataResult : Result<ImageMetadata>
{
    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private ImageMetadataResult(bool isSuccess, ImageMetadata? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful ImageMetadataResult with metadata.</summary>
    public static ImageMetadataResult FromSuccess(ImageMetadata metadata, string? message = null) => new(true, metadata) { Message = message };

    /// <summary>Creates a failed ImageMetadataResult from an exception.</summary>
    public static ImageMetadataResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Creates a failed ImageMetadataResult with a custom error message.</summary>
    public static ImageMetadataResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }
}