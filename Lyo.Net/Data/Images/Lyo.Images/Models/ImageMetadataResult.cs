using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Images.Models;

/// <summary>Metadata-read outcome with image-specific fields.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ImageMetadataResult : Result<ImageMetadata>
{
    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    private ImageMetadataResult(bool isSuccess, ImageMetadata? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful ImageMetadataResult with metadata.</summary>
    public static ImageMetadataResult FromSuccess(ImageMetadata metadata, string? message = null) => new(true, metadata) { Message = message };

    /// <summary>Failed ImageMetadataResult from an exception.</summary>
    public static ImageMetadataResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Failed ImageMetadataResult with a custom error message.</summary>
    public static ImageMetadataResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }

    public override string ToString() => IsSuccess ? $"ImageMetadataResult: {Data}" : $"ImageMetadataResult: failed, errors={Errors?.Count ?? 0}";
}