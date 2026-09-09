using Lyo.Result;

namespace Lyo.Encryption.Models;

/// <summary>Outcome of a decrypt call, including decryption-specific fields.</summary>
public sealed record DecryptionResult : Result<byte[]>
{
    /// <summary>Key id used when decrypting.</summary>
    public string? KeyId { get; init; }

    /// <summary>Key version used when decrypting.</summary>
    public string? KeyVersion { get; init; }

    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    /// <summary>Decrypted payload size in bytes.</summary>
    public int? DecryptedSize { get; init; }

    private DecryptionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful DecryptionResult carrying plaintext.</summary>
    public static DecryptionResult FromSuccess(byte[] decryptedData, string? keyId = null, string? keyVersion = null, string? message = null)
        => new(true, decryptedData) {
            KeyId = keyId,
            KeyVersion = keyVersion,
            Message = message,
            DecryptedSize = decryptedData.Length
        };

    /// <summary>Failed DecryptionResult built from an exception.</summary>
    public static DecryptionResult FromException(Exception exception, byte[]? encryptedData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, encryptedData, [error]);
    }

    /// <summary>Failed DecryptionResult with a caller-supplied message.</summary>
    public static DecryptionResult FromError(string errorMessage, string errorCode, byte[]? encryptedData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, encryptedData, [error]);
    }
}