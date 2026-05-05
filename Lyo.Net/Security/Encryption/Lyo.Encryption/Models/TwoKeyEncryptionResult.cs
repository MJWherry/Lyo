namespace Lyo.Encryption.Models;

/// <summary>Minimal envelope result: ciphertext, wrapped DEK, and an integer key version (legacy / compact shape).</summary>
/// <remarks>For full keystore metadata (string key id and version, salt), use <see cref="Lyo.Encryption.TwoKey.TwoKeyEncryptionResult" />.</remarks>
/// <param name="EncryptedData">Payload encrypted with the data encryption key.</param>
/// <param name="EncryptedDataEncryptionKey">The DEK encrypted with the key encryption key.</param>
/// <param name="KeyVersion">Numeric key version marker.</param>
public sealed record TwoKeyEncryptionResult(byte[] EncryptedData, byte[] EncryptedDataEncryptionKey, int KeyVersion)
{
    public long TotalSize => EncryptedData.Length + EncryptedDataEncryptionKey.Length;
}