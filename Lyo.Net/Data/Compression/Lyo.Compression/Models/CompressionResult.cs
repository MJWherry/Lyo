using Lyo.Result;

namespace Lyo.Compression.Models;

/// <summary>Outcome of a compress call, plus compression-specific fields.</summary>
public sealed record CompressionResult : Result<byte[]>
{
    /// <summary>Timing and size stats for the compress.</summary>
    public CompressionInfo? CompressionInfo { get; init; }

    /// <summary>Optional result message.</summary>
    public string? Message { get; init; }

    private CompressionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful CompressionResult with compressed data.</summary>
    public static CompressionResult FromSuccess(byte[] compressedData, CompressionInfo compressionInfo, string? message = null)
        => new(true, compressedData) { CompressionInfo = compressionInfo, Message = message };

    /// <summary>Builds a failed CompressionResult from an exception.</summary>
    public static CompressionResult FromException(Exception exception, byte[]? originalData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, originalData, [error]);
    }

    /// <summary>Builds a failed CompressionResult with a custom error message.</summary>
    public static CompressionResult FromError(string errorMessage, string errorCode, byte[]? originalData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, originalData, [error]);
    }
}