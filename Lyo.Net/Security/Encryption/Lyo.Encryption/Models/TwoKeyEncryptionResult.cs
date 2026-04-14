namespace Lyo.Encryption.Models;

public sealed record TwoKeyEncryptionResult(byte[] EncryptedData, byte[] EncryptedDataEncryptionKey, int KeyVersion)
{
    public long TotalSize => EncryptedData.Length + EncryptedDataEncryptionKey.Length;
}