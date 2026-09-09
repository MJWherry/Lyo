using Lyo.Result;

namespace Lyo.Compression.Models;

/// <summary>Outcome of a decompress call, plus decompression-specific fields.</summary>
public sealed record DecompressionResult : Result<byte[]>
{
    /// <summary>Timing and size stats for the decompress.</summary>
    public DecompressionInfo? DecompressionInfo { get; init; }

    /// <summary>Optional result message.</summary>
    public string? Message { get; init; }

    private DecompressionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful DecompressionResult with decompressed data.</summary>
    public static DecompressionResult FromSuccess(byte[] decompressedData, DecompressionInfo decompressionInfo, string? message = null)
        => new(true, decompressedData) { DecompressionInfo = decompressionInfo, Message = message };

    /// <summary>Builds a failed DecompressionResult from an exception.</summary>
    public static DecompressionResult FromException(Exception exception, byte[]? compressedData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, compressedData, [error]);
    }

    /// <summary>Builds a failed DecompressionResult with a custom error message.</summary>
    public static DecompressionResult FromError(string errorMessage, string errorCode, byte[]? compressedData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, compressedData, [error]);
    }
}