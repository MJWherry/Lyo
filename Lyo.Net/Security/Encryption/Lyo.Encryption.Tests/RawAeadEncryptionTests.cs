using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.KeyStore;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

/// <summary>Raw AEAD encrypt/decrypt: foreign-client layout, no interchange with framed <see cref="IEncryptionService.Encrypt" />.</summary>
public sealed class RawAeadEncryptionTests
{
    [Fact]
    public void AesGcm_EncryptRawDecryptRaw_Roundtrips()
    {
        const string keyId = "raw-gcm";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "raw-gcm-secret");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "raw aead roundtrip"u8.ToArray();
        var encrypted = svc.EncryptRaw(plaintext, keyId);
        Assert.Equal(AesGcmHelper.NonceSize + AesGcmHelper.TagSize + plaintext.Length, encrypted.Length);
        Assert.Equal(plaintext, svc.DecryptRaw(encrypted, keyId));
    }

    [Fact]
    public void ChaCha20Poly1305_EncryptRawDecryptRaw_Roundtrips()
    {
        const string keyId = "raw-chacha";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "raw-chacha-secret");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var plaintext = "chacha raw roundtrip"u8.ToArray();
        var encrypted = svc.EncryptRaw(plaintext, keyId);
        Assert.Equal(ChaCha20Poly1305Helper.NonceSize + ChaCha20Poly1305Helper.TagSize + plaintext.Length, encrypted.Length);
        Assert.Equal(plaintext, svc.DecryptRaw(encrypted, keyId));
    }

    [Fact]
    public void AesGcm_EncryptRaw_Layout_DecryptableByHelper()
    {
        var key = TestData.Create(32);
        var keyStore = new LocalKeyStore();
        keyStore.AddKey("unused", "1", TestData.Create(32, TestData.Seed ^ 1));
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = Encoding.UTF8.GetBytes("foreign client payload");
        var raw = svc.EncryptRaw(plaintext, key: key);
        Assert.Equal(AesGcmHelper.NonceSize + AesGcmHelper.TagSize + plaintext.Length, raw.Length);
        var nonce = raw[..AesGcmHelper.NonceSize];
        var tag = raw.AsSpan(AesGcmHelper.NonceSize, AesGcmHelper.TagSize).ToArray();
        var ciphertext = raw[(AesGcmHelper.NonceSize + AesGcmHelper.TagSize)..];
        Assert.Equal(plaintext.Length, ciphertext.Length);
        var opened = AesGcmHelper.Decrypt(ciphertext, tag, key, nonce);
        Assert.Equal(plaintext, opened);
    }

    [Fact]
    public void AesGcm_FramedEncrypt_IsNotDecryptRaw()
    {
        const string keyId = "cross-format";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "cross-format-secret");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = Encoding.UTF8.GetBytes("hello world for format mismatch");
        var framed = svc.Encrypt(plaintext, keyId);
        Assert.Throws<DecryptionFailedException>(() => svc.DecryptRaw(framed, keyId));
    }

    [Fact]
    public void AesGcm_EncryptRaw_IsNotFramedDecrypt()
    {
        const string keyId = "cross-format-raw";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "cross-format-secret");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = Encoding.UTF8.GetBytes("hello world for format mismatch");
        var raw = svc.EncryptRaw(plaintext, keyId);
        var ex = Record.Exception(() => svc.Decrypt(raw, keyId));
        Assert.True(ex is InvalidDataException or DecryptionFailedException or ArgumentOutsideRangeException, ex?.GetType().FullName);
    }

    [Fact]
    public void AesGcm_DecryptRaw_AfterRotation_NeedsKeyVersion()
    {
        const string keyId = "raw-rotate";
        var keyStore = new LocalKeyStore();
        var version1 = keyStore.UpdateKeyFromString(keyId, "raw-rotate-v1");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "version pinned only by caller"u8.ToArray();
        var encrypted = svc.EncryptRaw(plaintext, keyId);
        keyStore.UpdateKeyFromString(keyId, "raw-rotate-v2");
        Assert.Throws<DecryptionFailedException>(() => svc.DecryptRaw(encrypted, keyId));
        Assert.Equal(plaintext, svc.DecryptRaw(encrypted, keyId, keyVersion: version1));
    }

    [Fact]
    public void AesGcm_DecryptRaw_AadMismatch_Throws()
    {
        var key = TestData.Create(32);
        var keyStore = new LocalKeyStore();
        keyStore.AddKey("unused", "1", TestData.Create(32, TestData.Seed ^ 2));
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = "aad bound"u8.ToArray();
        var aad = "bound-aad"u8.ToArray();
        var encrypted = svc.EncryptRaw(plaintext, key: key, associatedData: aad);
        Assert.Throws<DecryptionFailedException>(() => svc.DecryptRaw(encrypted, key: key));
        Assert.Equal(plaintext, svc.DecryptRaw(encrypted, key: key, associatedData: aad));
    }
}
