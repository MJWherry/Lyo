using System.Diagnostics;

namespace Lyo.Encryption.Models;

/// <summary>Compact envelope: ciphertext, wrapped DEK, and a numeric key version (legacy shape).</summary>
/// <remarks>Full keystore metadata (string key id/version, salt) lives on <see cref="Lyo.Encryption.TwoKey.TwoKeyEncryptionResult" />.</remarks>
/// <param name="EncryptedData">Payload sealed with the data encryption key.</param>
/// <param name="EncryptedDataEncryptionKey">DEK sealed with the key encryption key.</param>
/// <param name="KeyVersion">Numeric key-version marker.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TwoKeyEncryptionResult(byte[] EncryptedData, byte[] EncryptedDataEncryptionKey, int KeyVersion)
{
    public long TotalSize => EncryptedData.Length + EncryptedDataEncryptionKey.Length;

    /// <summary>Key version and ciphertext lengths. Key material is omitted.</summary>
    public override string ToString() => $"Version={KeyVersion} Data={EncryptedData.Length} WrappedDek={EncryptedDataEncryptionKey.Length}";
}