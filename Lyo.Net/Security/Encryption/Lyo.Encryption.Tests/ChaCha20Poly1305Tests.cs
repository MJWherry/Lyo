using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class ChaCha20Poly1305Tests
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
        var nonce = RandomNumberGenerator.GetBytes(ChaCha20Poly1305Helper.NonceSize);
        var (cipher, tag) = ChaCha20Poly1305Helper.Encrypt(plaintext, key, nonce);
        var result = ChaCha20Poly1305Helper.Decrypt(cipher, tag, key, nonce);
        Assert.Equal("hello world", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void Service_EncryptDecrypt_WithProvidedKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var key = DeriveKey("k2");
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal("payload", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Service_EncryptDecrypt_WithKeyStore()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "mypassword");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
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
        var nonce = RandomNumberGenerator.GetBytes(ChaCha20Poly1305Helper.NonceSize);
        var (cipher, tag) = ChaCha20Poly1305Helper.Encrypt(plaintext, key, nonce);
        // tamper tag
        tag[0] ^= 0xFF;
        Assert.Throws<AuthenticationTagMismatchException>(() => ChaCha20Poly1305Helper.Decrypt(cipher, tag, key, nonce));
    }

    [Fact]
    public void Service_Decrypt_WithWrongKey_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var key = DeriveKey("k2");
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: key);
        var wrongKey = DeriveKey("other");
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(enc, key: wrongKey));
    }

    [Fact]
    public void Helper_Constants_AreExpected()
    {
        Assert.Equal(12, ChaCha20Poly1305Helper.NonceSize);
        Assert.Equal(16, ChaCha20Poly1305Helper.TagSize);
    }

    [Fact]
    public void Helper_EncryptDecrypt_EmptyPlaintext()
    {
        var key = DeriveKey("k");
        var nonce = RandomNumberGenerator.GetBytes(ChaCha20Poly1305Helper.NonceSize);
        var (cipher, tag) = ChaCha20Poly1305Helper.Encrypt([], key, nonce);
        var pt = ChaCha20Poly1305Helper.Decrypt(cipher, tag, key, nonce);
        Assert.Empty(pt);
    }

    [Fact]
    public void Service_EmptyPayload_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        Assert.Throws<ArgumentOutsideRangeException>(() => svc.Encrypt([], keyId));
    }

    [Fact]
    public void FileExtension_IsCorrect()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        Assert.Equal(FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension, svc.FileExtension);
    }

    [Fact]
    public void Encrypt_WithKeyId_StoresKeyIdAndVersionInHeader()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var expectedVersion = keyStore.UpdateKeyFromString(keyId, "password-v1");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
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
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
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
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);

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
}