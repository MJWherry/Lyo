using Lyo.Common;

namespace Lyo.Images.Models;

/// <summary>Result of an image palette extraction operation.</summary>
public sealed record ImagePaletteResult : Result<ImagePalette>
{
    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private ImagePaletteResult(bool isSuccess, ImagePalette? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful ImagePaletteResult with palette colors.</summary>
    public static ImagePaletteResult FromSuccess(ImagePalette palette, string? message = null) => new(true, palette) { Message = message };

    /// <summary>Creates a failed ImagePaletteResult from an exception.</summary>
    public static ImagePaletteResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Creates a failed ImagePaletteResult with a custom error message.</summary>
    public static ImagePaletteResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }
}