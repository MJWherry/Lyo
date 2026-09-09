namespace Lyo.Encryption.Exceptions;

/// <summary>Raised when decrypt cannot succeed (wrong key, damaged payload, or authentication failure).</summary>
public class DecryptionFailedException : EncryptionException
{
    public DecryptionFailedException()
        : base("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.") { }

    public DecryptionFailedException(string message)
        : base(message) { }

    public DecryptionFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}