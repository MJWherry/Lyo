namespace Lyo.Encryption.Exceptions;

/// <summary>Root exception type for encryption failures.</summary>
public class EncryptionException : Exception
{
    public EncryptionException() { }

    public EncryptionException(string message)
        : base(message) { }

    public EncryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}