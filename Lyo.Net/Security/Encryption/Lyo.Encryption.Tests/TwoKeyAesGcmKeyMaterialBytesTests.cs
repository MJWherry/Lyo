using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

/// <summary>Two-key envelope with non-default AES-GCM DEK sizes; asserts <see cref="TwoKeyEncryptionResult.DekKeyMaterialBytes"/>.</summary>
public class TwoKeyAesGcmKeyMaterialBytesTests
{
    [Theory]
    [InlineData(AesGcmKeySizeBits.Bits128, 16)]
    [InlineData(AesGcmKeySizeBits.Bits192, 24)]
    [InlineData(AesGcmKeySizeBits.Bits256, 32)]
    public void EncryptDecrypt_Roundtrip_DeclaresDekKeyMaterialBytes(AesGcmKeySizeBits keySizeBits, int expectedMaterialBytes)
    {
        const string keyId = "kek";
        var keyStore = new LocalKeyStore();
        keyStore.AddKey(keyId, "1", RandomNumberGenerator.GetBytes(expectedMaterialBytes));
        var aes = new AesGcmEncryptionService(keyStore, keySizeBits);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aes, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("two-key material size");
        var result = svc.Encrypt(plaintext, keyId);
        Assert.Equal(expectedMaterialBytes, result.DekKeyMaterialBytes);
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId);
        Assert.Equal(plaintext, decrypted);
    }
}
