using Lyo.Common;

namespace Lyo.Compression.Models;

/// <summary>Result of a decompression operation with decompression-specific properties.</summary>
public sealed record DecompressionResult : Result<byte[]>
{
    /// <summary>The decompression information.</summary>
    public DecompressionInfo? DecompressionInfo { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private DecompressionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful DecompressionResult with decompressed data.</summary>
    public static DecompressionResult FromSuccess(byte[] decompressedData, DecompressionInfo decompressionInfo, string? message = null)
        => new(true, decompressedData) { DecompressionInfo = decompressionInfo, Message = message };

    /// <summary>Creates a failed DecompressionResult from an exception.</summary>
    public static DecompressionResult FromException(Exception exception, byte[]? compressedData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, compressedData, [error]);
    }

    /// <summary>Creates a failed DecompressionResult with a custom error message.</summary>
    public static DecompressionResult FromError(string errorMessage, string errorCode, byte[]? compressedData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, compressedData, [error]);
    }
}