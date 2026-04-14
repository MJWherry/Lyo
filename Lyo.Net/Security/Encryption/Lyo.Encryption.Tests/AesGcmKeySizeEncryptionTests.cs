using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class AesGcmKeySizeEncryptionTests
{
    [Theory]
    [InlineData(AesGcmKeySizeBits.Bits128)]
    [InlineData(AesGcmKeySizeBits.Bits192)]
    [InlineData(AesGcmKeySizeBits.Bits256)]
    public void Service_EncryptDecrypt_WithDirectKey_MatchesKeySize(AesGcmKeySizeBits keySizeBits)
    {
        var len = keySizeBits.GetKeyLengthBytes();
        var key = RandomNumberGenerator.GetBytes(len);
        var keyStore = new LocalKeyStore();
        var svc = new AesGcmEncryptionService(keyStore, keySizeBits);
        var plaintext = "aes-gcm sized key"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal(plaintext, dec);
    }

    [Theory]
    [InlineData(AesGcmKeySizeBits.Bits128)]
    [InlineData(AesGcmKeySizeBits.Bits192)]
    [InlineData(AesGcmKeySizeBits.Bits256)]
    public void Service_EncryptDecrypt_WithKeyId_UsesKeystoreKeyLength(AesGcmKeySizeBits keySizeBits)
    {
        const string keyId = "dek-test";
        var len = keySizeBits.GetKeyLengthBytes();
        var keyStore = new LocalKeyStore();
        keyStore.AddKey(keyId, "1", RandomNumberGenerator.GetBytes(len));
        var svc = new AesGcmEncryptionService(keyStore, keySizeBits);
        var enc = svc.Encrypt("keystore key length"u8.ToArray(), keyId);
        var dec = svc.Decrypt(enc, keyId);
        Assert.Equal("keystore key length", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Service_Encrypt_WithWrongKeyLengthFromKeystore_Throws()
    {
        const string keyId = "k";
        var keyStore = new LocalKeyStore();
        keyStore.AddKey(keyId, "1", RandomNumberGenerator.GetBytes(32));
        var svc = new AesGcmEncryptionService(keyStore, AesGcmKeySizeBits.Bits128);
        var ex = Assert.Throws<ArgumentException>(() => svc.Encrypt("x"u8.ToArray(), keyId));
        Assert.Contains("16", ex.Message);
    }

    [Fact]
    public void RequiredKeyBytes_MatchesConfiguredSize()
    {
        var ks = new LocalKeyStore();
        Assert.Equal(16, new AesGcmEncryptionService(ks, AesGcmKeySizeBits.Bits128).RequiredKeyBytes);
        Assert.Equal(24, new AesGcmEncryptionService(ks, AesGcmKeySizeBits.Bits192).RequiredKeyBytes);
        Assert.Equal(32, new AesGcmEncryptionService(ks, AesGcmKeySizeBits.Bits256).RequiredKeyBytes);
    }
}
