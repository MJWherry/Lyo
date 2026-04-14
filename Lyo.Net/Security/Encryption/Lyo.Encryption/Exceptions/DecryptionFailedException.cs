namespace Lyo.Encryption.Exceptions;

/// <summary> Exception thrown when decryption fails (e.g., wrong key, corrupted data, authentication failure). </summary>
public class DecryptionFailedException : EncryptionException
{
    public DecryptionFailedException()
        : base("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.") { }

    public DecryptionFailedException(string message)
        : base(message) { }

    public DecryptionFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}