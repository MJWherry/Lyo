using Lyo.Common;

namespace Lyo.Encryption.Models;

/// <summary>Result of a decryption operation with decryption-specific properties.</summary>
public sealed record DecryptionResult : Result<byte[]>
{
    /// <summary>The key ID used for decryption.</summary>
    public string? KeyId { get; init; }

    /// <summary>The key version used for decryption.</summary>
    public string? KeyVersion { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    /// <summary>The size of the decrypted data in bytes.</summary>
    public int? DecryptedSize { get; init; }

    private DecryptionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful DecryptionResult with decrypted data.</summary>
    public static DecryptionResult FromSuccess(byte[] decryptedData, string? keyId = null, string? keyVersion = null, string? message = null)
        => new(true, decryptedData) {
            KeyId = keyId,
            KeyVersion = keyVersion,
            Message = message,
            DecryptedSize = decryptedData.Length
        };

    /// <summary>Creates a failed DecryptionResult from an exception.</summary>
    public static DecryptionResult FromException(Exception exception, byte[]? encryptedData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, encryptedData, [error]);
    }

    /// <summary>Creates a failed DecryptionResult with a custom error message.</summary>
    public static DecryptionResult FromError(string errorMessage, string errorCode, byte[]? encryptedData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, encryptedData, [error]);
    }
}