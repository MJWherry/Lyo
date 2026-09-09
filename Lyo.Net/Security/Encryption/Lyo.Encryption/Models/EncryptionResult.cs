using Lyo.Result;

namespace Lyo.Encryption.Models;

/// <summary>Outcome of an encrypt call, including encryption-specific fields.</summary>
public sealed record EncryptionResult : Result<byte[]>
{
    /// <summary>Key id used when encrypting.</summary>
    public string? KeyId { get; init; }

    /// <summary>Key version used when encrypting.</summary>
    public string? KeyVersion { get; init; }

    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    /// <summary>Encrypted payload size in bytes.</summary>
    public int? EncryptedSize { get; init; }

    private EncryptionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful EncryptionResult carrying ciphertext.</summary>
    public static EncryptionResult FromSuccess(byte[] encryptedData, string? keyId = null, string? keyVersion = null, string? message = null)
        => new(true, encryptedData) {
            KeyId = keyId,
            KeyVersion = keyVersion,
            Message = message,
            EncryptedSize = encryptedData.Length
        };

    /// <summary>Failed EncryptionResult built from an exception.</summary>
    public static EncryptionResult FromException(Exception exception, byte[]? originalData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, originalData, [error]);
    }

    /// <summary>Failed EncryptionResult with a caller-supplied message.</summary>
    public static EncryptionResult FromError(string errorMessage, string errorCode, byte[]? originalData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, originalData, [error]);
    }
}