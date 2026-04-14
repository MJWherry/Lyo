namespace Lyo.Keystore.Exceptions;

/// <summary> Exception thrown when a key is invalid (wrong size, format, or strength). </summary>
public class InvalidKeyException : EncryptionKeyException
{
    public InvalidKeyException() { }

    public InvalidKeyException(string message)
        : base(message) { }

    public InvalidKeyException(string message, Exception innerException)
        : base(message, innerException) { }
}