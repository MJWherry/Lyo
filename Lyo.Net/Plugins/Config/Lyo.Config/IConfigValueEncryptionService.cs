namespace Lyo.Config;

/// <summary>
/// Wraps encrypt/decrypt of config JSON payloads at rest. Implemented by the API/store host; clients only send <see cref="ConfigDefinitionRecord.IsEncrypted" /> plus plaintext.
/// </summary>
public interface IConfigValueEncryptionService
{
    /// <summary>True when an <c>IEncryptionService</c> was registered on this host.</summary>
    bool IsEncryptionEnabled { get; }

    /// <summary>True if ciphertext is stored (non-null marker, including empty <c>[]</c> meaning “encrypt this plaintext”).</summary>
    bool UsesEncryptedStorage(byte[]? encryptedValueMarker);

    /// <summary>Turns into ciphertext: <paramref name="json" /> into <paramref name="encryptedValue" /> and clears JSON when encryption is enabled and a marker is set.</summary>
    void EncryptValue(ref string? json, ref byte[]? encryptedValue);

    /// <summary>Turns ciphertext into ciphertext to JSON. Returns null when there is nothing to decrypt or decryption fails.</summary>
    string? DecryptValue(byte[]? encryptedValue);

    /// <summary>Gives a masked placeholder when the value is stored encrypted; otherwise <paramref name="json" />.</summary>
    string? MaskValue(string? json, byte[]? encryptedValueMarker);
}
