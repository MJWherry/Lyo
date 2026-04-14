namespace Lyo.Encryption.TwoKey;

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