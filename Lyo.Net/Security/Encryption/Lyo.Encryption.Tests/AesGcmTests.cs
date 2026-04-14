using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Exceptions;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

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
        var nonce = RandomNumberGenerator.GetBytes(AesGcmHelper.NonceSize);
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
        var nonce = RandomNumberGenerator.GetBytes(AesGcmHelper.NonceSize);
        var (cipher, tag) = AesGcmHelper.Encrypt(plaintext, key, nonce);
        // tamper tag
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

        // Verify header format: [FormatVersion: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion][nonceLength: 4][nonce][tag][ciphertext]
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);

        // Read format version
        var version = br.ReadByte();
        Assert.Equal(1, version);

        // Read keyId length
        var keyIdLength = br.ReadInt32();
        Assert.True(keyIdLength > 0);

        // Read keyId
        var keyIdBytes = br.ReadBytes(keyIdLength);
        var storedKeyId = Encoding.UTF8.GetString(keyIdBytes);
        Assert.Equal(keyId, storedKeyId);

        // Read keyVersion
        var keyVersion = br.ReadString();
        Assert.Equal(expectedVersion, keyVersion);
    }

    [Fact]
    public void Decrypt_ReadsKeyIdAndVersionFromHeader_AutomaticallyUsesCorrectKey()
    {
        const string keyId = "rotation-key";
        var keyStore = new LocalKeyStore();

        // Encrypt with version 1
        keyStore.UpdateKeyFromString(keyId, "password-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "version 1 data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, keyId);

        // Rotate to version 2
        keyStore.UpdateKeyFromString(keyId, "password-v2");

        // Decrypt without specifying version - should automatically use version from header
        var decrypted = svc.Decrypt(encrypted, keyId);
        Assert.Equal("version 1 data", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void KeyRotation_EncryptWithV1_DecryptAfterRotationToV2_Works()
    {
        const string keyId = "rotation-test";
        var keyStore = new LocalKeyStore();

        // Setup version 1
        keyStore.UpdateKeyFromString(keyId, "key-v1");
        var svc = new AesGcmEncryptionService(keyStore);

        // Encrypt data with version 1
        var dataV1 = "data encrypted with v1"u8.ToArray();
        var encryptedV1 = svc.Encrypt(dataV1, keyId);

        // Rotate to version 2
        keyStore.UpdateKeyFromString(keyId, "key-v2");

        // Encrypt new data with version 2
        var dataV2 = "data encrypted with v2"u8.ToArray();
        var encryptedV2 = svc.Encrypt(dataV2, keyId);

        // Verify both can be decrypted
        var decryptedV1 = svc.Decrypt(encryptedV1, keyId);
        var decryptedV2 = svc.Decrypt(encryptedV2, keyId);
        Assert.Equal("data encrypted with v1", Encoding.UTF8.GetString(decryptedV1));
        Assert.Equal("data encrypted with v2", Encoding.UTF8.GetString(decryptedV2));

        // Verify encrypted data is different
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

        // Verify header format
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);

        // Read format version
        var version = br.ReadByte();
        Assert.Equal(1, version);

        // Read keyId length (should be 0 when using direct key)
        var keyIdLength = br.ReadInt32();
        Assert.Equal(0, keyIdLength);

        // Read keyVersion (should be empty when using direct key)
        var keyVersion = br.ReadString();
        Assert.Equal("", keyVersion);
    }

    [Fact]
    public void Decrypt_WithWrongKeyVersion_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();

        // Encrypt with version 1
        var version1 = keyStore.UpdateKeyFromString(keyId, "key-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "test data"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, keyId);

        // Add version 2 and set it as current
        keyStore.UpdateKeyFromString(keyId, "key-v2");

        // Now remove version 1 (version 2 is current, so we can remove version 1)
        keyStore.RemoveKey(keyId, version1);

        // Try to decrypt - should fail because version 1 key is missing
        Assert.ThrowsAny<InvalidOperationException>(() => svc.Decrypt(encrypted, keyId));
    }
}