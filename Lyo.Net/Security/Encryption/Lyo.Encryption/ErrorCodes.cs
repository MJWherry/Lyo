namespace Lyo.Encryption;

/// <summary>Numeric codes emitted by Encryption services.</summary>
public static class EncryptionErrorCodes
{
    /// <summary>Encrypt did not succeed.</summary>
    public const string EncryptFailed = "ENCRYPTION_FAILED";

    /// <summary>Decrypt did not succeed.</summary>
    public const string DecryptFailed = "DECRYPTION_FAILED";

    /// <summary>Caller cancelled the operation.</summary>
    public const string OperationCancelled = "ENCRYPTION_OPERATION_CANCELLED";

    /// <summary>No encryption key could be resolved.</summary>
    public const string NoKeyAvailable = "ENCRYPTION_NO_KEY_AVAILABLE";

    /// <summary>Key length is not valid.</summary>
    public const string InvalidKeySize = "ENCRYPTION_INVALID_KEY_SIZE";

    /// <summary>Input exceeds the configured maximum size.</summary>
    public const string InputTooLarge = "ENCRYPTION_INPUT_TOO_LARGE";

    /// <summary>Input is below the configured minimum size.</summary>
    public const string InputTooSmall = "ENCRYPTION_INPUT_TOO_SMALL";

    /// <summary>Ciphertext layout is not valid.</summary>
    public const string InvalidFormat = "ENCRYPTION_INVALID_FORMAT";

    /// <summary>A file I/O step failed.</summary>
    public const string FileOperationFailed = "ENCRYPTION_FILE_OPERATION_FAILED";
}