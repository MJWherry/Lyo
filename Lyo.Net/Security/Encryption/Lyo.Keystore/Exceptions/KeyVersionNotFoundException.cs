namespace Lyo.Keystore.Exceptions;

/// <summary> Exception thrown when a specific key version is not found. </summary>
public class KeyVersionNotFoundException : EncryptionKeyException
{
    public int KeyVersion { get; }

    public KeyVersionNotFoundException(int keyVersion)
        : base($"Key version {keyVersion} not found.")
        => KeyVersion = keyVersion;

    public KeyVersionNotFoundException(int keyVersion, Exception innerException)
        : base($"Key version {keyVersion} not found.", innerException)
        => KeyVersion = keyVersion;
}