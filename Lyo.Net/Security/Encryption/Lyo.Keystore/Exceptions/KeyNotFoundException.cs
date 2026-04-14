namespace Lyo.Keystore.Exceptions;

/// <summary> Exception thrown when a key is not found in the key store. </summary>
public class KeyNotFoundException : EncryptionKeyException
{
    public int? KeyVersion { get; }

    public KeyNotFoundException() { }

    public KeyNotFoundException(string message)
        : base(message) { }

    public KeyNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    public KeyNotFoundException(int keyVersion)
        : base($"Key version {keyVersion} not found.")
        => KeyVersion = keyVersion;

    public KeyNotFoundException(int keyVersion, Exception innerException)
        : base($"Key version {keyVersion} not found.", innerException)
        => KeyVersion = keyVersion;
}