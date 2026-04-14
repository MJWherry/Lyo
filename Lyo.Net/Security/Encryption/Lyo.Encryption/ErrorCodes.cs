namespace Lyo.Encryption;

/// <summary>Error codes used by Encryption services.</summary>
public static class EncryptionErrorCodes
{
    /// <summary>Failed to encrypt data.</summary>
    public const string EncryptFailed = "ENCRYPTION_FAILED";

    /// <summary>Failed to decrypt data.</summary>
    public const string DecryptFailed = "DECRYPTION_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "ENCRYPTION_OPERATION_CANCELLED";

    /// <summary>No encryption key available.</summary>
    public const string NoKeyAvailable = "ENCRYPTION_NO_KEY_AVAILABLE";

    /// <summary>Invalid key size.</summary>
    public const string InvalidKeySize = "ENCRYPTION_INVALID_KEY_SIZE";

    /// <summary>Input data exceeds maximum allowed size.</summary>
    public const string InputTooLarge = "ENCRYPTION_INPUT_TOO_LARGE";

    /// <summary>Input data is too small.</summary>
    public const string InputTooSmall = "ENCRYPTION_INPUT_TOO_SMALL";

    /// <summary>Invalid encrypted data format.</summary>
    public const string InvalidFormat = "ENCRYPTION_INVALID_FORMAT";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "ENCRYPTION_FILE_OPERATION_FAILED";
}