using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Exceptions;
using Lyo.KeyStore;
using Lyo.KeyStore.KeyDerivation;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

public class AesGcmTests
{
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    [Fact]
    public void DeriveKey_Is32Bytes()
    {
        var key = DeriveKey("password");
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_Helper()
    {
        var plaintext = "hello world"u8.ToArray();
        var key = DeriveKey("k");
        var nonce = TestData.Create(AesGcmHelper.NonceSize);
        var (cipher, tag) = AesGcmHelper.Encrypt(plaintext, key, nonce);
        var result = AesGcmHelper.Decrypt(cipher, tag, key, nonce);
        Assert.Equal("hello world", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void Service_EncryptDecrypt_WithProvidedKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var key = DeriveKey("k2");
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal("payload", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Service_EncryptDecrypt_WithOptionsKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "mypassword");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "payload2"u8.ToArray();
        var enc = svc.Encrypt(plaintext, keyId);
        var dec = svc.Decrypt(enc, keyId);
        Assert.Equal("payload2", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Helper_Decrypt_TamperedTag_Throws()
    {
        var plaintext = "msg"u8.ToArray();
        var key = DeriveKey("k");
        var nonce = TestData.Create(AesGcmHelper.NonceSize);
        var (cipher, tag) = AesGcmHelper.Encrypt(plaintext, key, nonce);
        // corrupt the tag
        tag[0] ^= 0xFF;
        Assert.Throws<AuthenticationTagMismatchException>(() => AesGcmHelper.Decrypt(cipher, tag, key, nonce));
    }

    [Fact]
    public void Service_Decrypt_WithWrongKey_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var key = DeriveKey("k2");
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: key);
        var wrongKey = DeriveKey("other");
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(enc, key: wrongKey));
    }

    [Fact]
    public void Encrypt_WithKeyId_StoresKeyIdAndVersionInHeader()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var expectedVersion = keyStore.UpdateKeyFromString(keyId, "password-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "test data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, keyId);

        // header layout: [FormatVersion: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion][nonceLength: 4][nonce][tag][ciphertext]
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);

        // format version
        var version = br.ReadByte();
        Assert.Equal(1, version);

        // keyId length
        var keyIdLength = br.ReadInt32();
        Assert.True(keyIdLength > 0);

        // keyId bytes
        var keyIdBytes = br.ReadBytes(keyIdLength);
        var storedKeyId = Encoding.UTF8.GetString(keyIdBytes);
        Assert.Equal(keyId, storedKeyId);

        // keyVersion
        var keyVersion = br.ReadString();
        Assert.Equal(expectedVersion, keyVersion);
    }

    [Fact]
    public void Decrypt_ReadsKeyIdAndVersionFromHeader_AutomaticallyUsesCorrectKey()
    {
        const string keyId = "rotation-key";
        var keyStore = new LocalKeyStore();

        // encrypt under version 1
        keyStore.UpdateKeyFromString(keyId, "password-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "version 1 data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, keyId);

        // rotate to version 2
        keyStore.UpdateKeyFromString(keyId, "password-v2");

        // decrypt with no version argument — header version is used automatically
        var decrypted = svc.Decrypt(encrypted, keyId);
        Assert.Equal("version 1 data", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void KeyRotation_EncryptWithV1_DecryptAfterRotationToV2_Works()
    {
        const string keyId = "rotation-test";
        var keyStore = new LocalKeyStore();

        // prepare version 1
        keyStore.UpdateKeyFromString(keyId, "key-v1");
        var svc = new AesGcmEncryptionService(keyStore);

        // encrypt under version 1
        var dataV1 = "data encrypted with v1"u8.ToArray();
        var encryptedV1 = svc.Encrypt(dataV1, keyId);

        // rotate to version 2
        keyStore.UpdateKeyFromString(keyId, "key-v2");

        // encrypt new data under version 2
        var dataV2 = "data encrypted with v2"u8.ToArray();
        var encryptedV2 = svc.Encrypt(dataV2, keyId);

        // both ciphertexts must decrypt
        var decryptedV1 = svc.Decrypt(encryptedV1, keyId);
        var decryptedV2 = svc.Decrypt(encryptedV2, keyId);
        Assert.Equal("data encrypted with v1", Encoding.UTF8.GetString(decryptedV1));
        Assert.Equal("data encrypted with v2", Encoding.UTF8.GetString(decryptedV2));

        // ciphertexts must differ
        Assert.NotEqual(encryptedV1, encryptedV2);
    }

    [Fact]
    public void Encrypt_WithDirectKey_DoesNotStoreKeyIdInHeader()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "dummy");
        var svc = new AesGcmEncryptionService(keyStore);
        var key = DeriveKey("direct-key");
        var plaintext = "test data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, key: key);

        // check header layout
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);

        // format version
        var version = br.ReadByte();
        Assert.Equal(1, version);

        // keyId length (0 when the key is passed inline)
        var keyIdLength = br.ReadInt32();
        Assert.Equal(0, keyIdLength);

        // keyVersion (empty when the key is passed inline)
        var keyVersion = br.ReadString();
        Assert.Equal("", keyVersion);
    }

    [Fact]
    public void Decrypt_WithWrongKeyVersion_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();

        // encrypt under version 1
        var version1 = keyStore.UpdateKeyFromString(keyId, "key-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "test data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, keyId);

        // add version 2 and make it current
        keyStore.UpdateKeyFromString(keyId, "key-v2");

        // drop version 1 now that version 2 is current
        keyStore.RemoveKey(keyId, version1);

        // decrypt must fail because the version 1 key is gone
        Assert.ThrowsAny<InvalidOperationException>(() => svc.Decrypt(encrypted, keyId));
    }
}