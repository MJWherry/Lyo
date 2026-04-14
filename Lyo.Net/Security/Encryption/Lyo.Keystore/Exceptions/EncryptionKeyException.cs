namespace Lyo.Keystore.Exceptions;

public class EncryptionKeyException : Exception
{
    public EncryptionKeyException() { }

    public EncryptionKeyException(string message)
        : base(message) { }

    public EncryptionKeyException(string message, Exception innerException)
        : base(message, innerException) { }
}