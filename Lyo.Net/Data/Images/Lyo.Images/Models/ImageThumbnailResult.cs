using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Result;

namespace Lyo.Images.Models;

/// <summary>Thumbnail-generation outcome with image-specific fields.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ImageThumbnailResult : Result<byte[]>
{
    /// <summary>Thumbnail max width.</summary>
    public int? MaxWidth { get; init; }

    /// <summary>Thumbnail max height.</summary>
    public int? MaxHeight { get; init; }

    /// <summary>Image format used.</summary>
    public ImageFormat? Format { get; init; }

    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    private ImageThumbnailResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful ImageThumbnailResult with thumbnail bytes.</summary>
    public static ImageThumbnailResult FromSuccess(byte[] thumbnailBytes, int? maxWidth = null, int? maxHeight = null, ImageFormat? format = null, string? message = null)
        => new(true, thumbnailBytes) {
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
            Format = format,
            Message = message
        };

    /// <summary>Failed ImageThumbnailResult from an exception.</summary>
    public static ImageThumbnailResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Failed ImageThumbnailResult with a custom error message.</summary>
    public static ImageThumbnailResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }

    public override string ToString()
        => IsSuccess ? $"ImageThumbnailResult: {Data?.Length ?? 0} bytes, {MaxWidth}x{MaxHeight}, format={Format}" : $"ImageThumbnailResult: failed, errors={Errors?.Count ?? 0}";
}