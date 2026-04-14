using Lyo.Common;

namespace Lyo.Encryption.Models;

/// <summary>Result of an encryption operation with encryption-specific properties.</summary>
public sealed record EncryptionResult : Result<byte[]>
{
    /// <summary>The key ID used for encryption.</summary>
    public string? KeyId { get; init; }

    /// <summary>The key version used for encryption.</summary>
    public string? KeyVersion { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    /// <summary>The size of the encrypted data in bytes.</summary>
    public int? EncryptedSize { get; init; }

    private EncryptionResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful EncryptionResult with encrypted data.</summary>
    public static EncryptionResult FromSuccess(byte[] encryptedData, string? keyId = null, string? keyVersion = null, string? message = null)
        => new(true, encryptedData) {
            KeyId = keyId,
            KeyVersion = keyVersion,
            Message = message,
            EncryptedSize = encryptedData.Length
        };

    /// <summary>Creates a failed EncryptionResult from an exception.</summary>
    public static EncryptionResult FromException(Exception exception, byte[]? originalData = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, originalData, [error]);
    }

    /// <summary>Creates a failed EncryptionResult with a custom error message.</summary>
    public static EncryptionResult FromError(string errorMessage, string errorCode, byte[]? originalData = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, originalData, [error]);
    }
}