using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Images.Models;

/// <summary>Outcome of palette extraction.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ImagePaletteResult : Result<ImagePalette>
{
    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    private ImagePaletteResult(bool isSuccess, ImagePalette? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful ImagePaletteResult with palette colors.</summary>
    public static ImagePaletteResult FromSuccess(ImagePalette palette, string? message = null) => new(true, palette) { Message = message };

    /// <summary>Failed ImagePaletteResult from an exception.</summary>
    public static ImagePaletteResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Failed ImagePaletteResult with a custom error message.</summary>
    public static ImagePaletteResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }

    public override string ToString() => IsSuccess ? $"ImagePaletteResult: success, colors={Data?.Colors.Count ?? 0}" : $"ImagePaletteResult: failed, errors={Errors?.Count ?? 0}";
}