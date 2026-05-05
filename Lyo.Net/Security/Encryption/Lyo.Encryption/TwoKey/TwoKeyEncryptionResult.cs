namespace Lyo.Encryption.TwoKey;

/// <summary>Ciphertext plus the DEK wrapped by the KEK, with keystore correlation metadata for decrypt and rotation.</summary>
/// <param name="EncryptedData">Payload encrypted with the data encryption key.</param>
/// <param name="EncryptedDataEncryptionKey">The DEK encrypted with the key encryption key.</param>
/// <param name="KeyId">Identifier used with <see cref="Lyo.Keystore.IKeyStore" /> when keys are not passed inline.</param>
/// <param name="KeyVersion">Version of the KEK (or key material) used for the wrapped DEK.</param>
/// <param name="KeyEncryptionKeySalt">Optional salt associated with KEK derivation or storage metadata.</param>
/// <param name="DekKeyMaterialBytes">DEK key material size in bytes (default 32).</param>
public sealed record TwoKeyEncryptionResult(
    byte[] EncryptedData,
    byte[] EncryptedDataEncryptionKey,
    string KeyId,
    string KeyVersion,
    byte[]? KeyEncryptionKeySalt = null,
    byte DekKeyMaterialBytes = 32)
{
    public long TotalSize => EncryptedData.Length + EncryptedDataEncryptionKey.Length;
}