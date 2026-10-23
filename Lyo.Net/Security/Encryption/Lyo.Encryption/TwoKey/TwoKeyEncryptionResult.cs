using System.Diagnostics;

namespace Lyo.Encryption.TwoKey;

/// <summary>Ciphertext and KEK-wrapped DEK, plus keystore correlation fields for decrypt and rotation.</summary>
/// <param name="EncryptedData">Payload sealed with the data encryption key.</param>
/// <param name="EncryptedDataEncryptionKey">DEK sealed with the key encryption key.</param>
/// <param name="KeyId">Id used with <see cref="Lyo.KeyStore.IKeyStore" /> when keys are not passed inline.</param>
/// <param name="KeyVersion">KEK (or key-material) version used to wrap the DEK.</param>
/// <param name="KeyEncryptionKeySalt">Optional salt tied to KEK derivation or storage metadata.</param>
/// <param name="DekKeyMaterialBytes">DEK material width in bytes (default 32).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TwoKeyEncryptionResult(
    byte[] EncryptedData,
    byte[] EncryptedDataEncryptionKey,
    string KeyId,
    string KeyVersion,
    byte[]? KeyEncryptionKeySalt = null,
    byte DekKeyMaterialBytes = 32)
{
    public long TotalSize => EncryptedData.Length + EncryptedDataEncryptionKey.Length;

    /// <summary>Key id, version, and ciphertext lengths. Key material is omitted.</summary>
    public override string ToString()
        => $"Id={KeyId} Ver={KeyVersion} DekBytes={DekKeyMaterialBytes} "
            + $"Data={EncryptedData.Length} Dek={EncryptedDataEncryptionKey.Length} Salt={KeyEncryptionKeySalt?.Length ?? 0}";
}