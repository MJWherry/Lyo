namespace Lyo.Encryption.Exceptions;

/// <summary> Base exception for all encryption-related errors. </summary>
public class EncryptionException : Exception
{
    public EncryptionException() { }

    public EncryptionException(string message)
        : base(message) { }

    public EncryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}