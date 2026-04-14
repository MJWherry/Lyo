using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class TwoKeyMixedEncryptionServiceTests
{
    private LocalKeyStore CreateKeyStore(string keyId = "test-key", string keyString = "test-kek")
    {
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, keyString);
        return keyStore;
    }

    private IEncryptionService CreateEncryptionService(EncryptionAlgorithm algorithm, IKeyStore keyStore)
        => algorithm switch {
            EncryptionAlgorithm.AesGcm => new AesGcmEncryptionService(keyStore),
            EncryptionAlgorithm.ChaCha20Poly1305 => new ChaCha20Poly1305EncryptionService(keyStore),
            EncryptionAlgorithm.AesCcm => new AesCcmEncryptionService(keyStore),
            EncryptionAlgorithm.AesSiv => new AesSivEncryptionService(keyStore),
            EncryptionAlgorithm.XChaCha20Poly1305 => new XChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new ArgumentException($"Unsupported encryption algorithm: {algorithm}", nameof(algorithm))
        };

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_Roundtrip_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes($"secret message encrypted with {dekAlgorithm} DEK and {kekAlgorithm} KEK");
        var result = svc.Encrypt(plaintext, "test-key");
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, "test-key");
        Assert.Equal(plaintext, decrypted);
        Assert.Equal($"secret message encrypted with {dekAlgorithm} DEK and {kekAlgorithm} KEK", Encoding.UTF8.GetString(decrypted));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecryptString_Roundtrip_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        ITwoKeyEncryptionService service = svc;
        var plaintext = $"Test string with {dekAlgorithm} DEK and {kekAlgorithm} KEK";
        var result = service.EncryptString(plaintext, "test-key");
        var decrypted = service.DecryptString(result.EncryptedData, result.EncryptedDataEncryptionKey, "test-key");
        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public async Task EncryptStreamAsync_DecryptStreamAsync_Roundtrip_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var inputText = $"This is a long stream content encrypted with {dekAlgorithm} DEK and {kekAlgorithm} KEK";
        var input = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
        var result = await svc.EncryptStreamAsync(input, "test-key").ConfigureAwait(false);
        using var output = new MemoryStream();
        await svc.DecryptStreamAsync(result, output, "test-key").ConfigureAwait(false);
        var decrypted = Encoding.UTF8.GetString(output.ToArray());
        Assert.Equal(inputText, decrypted);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public async Task EncryptToStreamAsync_DecryptToStreamAsync_Roundtrip_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var inputText = $"Stream encryption test with {dekAlgorithm} DEK and {kekAlgorithm} KEK";
        var input = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
        using var encryptedStream = new MemoryStream();
        await svc.EncryptToStreamAsync(input, encryptedStream, "test-key", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        await svc.DecryptToStreamAsync(encryptedStream, decryptedStream, "test-key", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decrypted = Encoding.UTF8.GetString(decryptedStream.ToArray());
        Assert.Equal(inputText, decrypted);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_WithExternalKek_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore("dummy-key");
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);

        // Use external KEK passed to methods (this overrides the key store KEK)
        var externalKek = "external-kek-key-32-bytes-long!!"u8.ToArray();
        var plaintext = Encoding.UTF8.GetBytes($"External KEK message with {dekAlgorithm} DEK and {kekAlgorithm} KEK");
        var result = svc.Encrypt(plaintext, kek: externalKek);
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, kek: externalKek);
        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_WithWrongKek_Throws_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStore(keyId, "correct-kek");
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var plaintext = "message"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);

        // Try to decrypt with wrong KEK (must be 32 bytes for AES-GCM / ChaCha KEK unwrap paths)
        var wrongKek = new byte[32];
        RandomNumberGenerator.Fill(wrongKek);
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId, wrongKek));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_EmptyData_Throws_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        const string keyId = "test-key";
        Assert.Throws<ArgumentException>(() => svc.Encrypt([], keyId));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_DifferentKeyVersions_Isolated_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        const string keyId1 = "test-key-1";
        const string keyId2 = "test-key-2";
        var keyStore1 = new LocalKeyStore();
        keyStore1.UpdateKeyFromString(keyId1, "kek-v1");
        var keyStore2 = new LocalKeyStore();
        keyStore2.UpdateKeyFromString(keyId2, "kek-v2");
        var dekService1 = CreateEncryptionService(dekAlgorithm, keyStore1);
        var kekService1 = CreateEncryptionService(kekAlgorithm, keyStore1);
        var dekService2 = CreateEncryptionService(dekAlgorithm, keyStore2);
        var kekService2 = CreateEncryptionService(kekAlgorithm, keyStore2);
        using var svc1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService1, kekService1, keyStore1);
        using var svc2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService2, kekService2, keyStore2);
        var plaintext = "versioned message"u8.ToArray();
        var result1 = svc1.Encrypt(plaintext, keyId1);
        var result2 = svc2.Encrypt(plaintext, keyId2);

        // Results should be different due to different KEKs
        Assert.NotEqual(result1.EncryptedData, result2.EncryptedData);
        Assert.NotEqual(result1.EncryptedDataEncryptionKey, result2.EncryptedDataEncryptionKey);
        Assert.Equal(keyId1, result1.KeyId);
        Assert.Equal(keyId2, result2.KeyId);
        Assert.NotNull(result1.KeyVersion);
        Assert.NotNull(result2.KeyVersion);
        Assert.NotEmpty(result1.KeyVersion);
        Assert.NotEmpty(result2.KeyVersion);

        // Each should decrypt with its own service
        var dec1 = svc1.Decrypt(result1.EncryptedData, result1.EncryptedDataEncryptionKey, keyId1);
        var dec2 = svc2.Decrypt(result2.EncryptedData, result2.EncryptedDataEncryptionKey, keyId2);
        Assert.Equal(plaintext, dec1);
        Assert.Equal(plaintext, dec2);

        // Cross-decryption should fail
        Assert.ThrowsAny<DecryptionFailedException>(() => svc1.Decrypt(result2.EncryptedData, result2.EncryptedDataEncryptionKey, keyId1));
        Assert.ThrowsAny<DecryptionFailedException>(() => svc2.Decrypt(result1.EncryptedData, result1.EncryptedDataEncryptionKey, keyId2));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void FileExtension_IsCorrect_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);

        // File extension should be based on DEK service + "2k"
        var expectedExtension = dekAlgorithm switch {
            EncryptionAlgorithm.AesGcm => FileTypeInfo.LyoAesGcm.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix,
            EncryptionAlgorithm.ChaCha20Poly1305 => FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix,
            EncryptionAlgorithm.AesCcm => FileTypeInfo.LyoAesCcm.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix,
            EncryptionAlgorithm.AesSiv => FileTypeInfo.LyoAesSiv.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix,
            EncryptionAlgorithm.XChaCha20Poly1305 => FileTypeInfo.LyoXChaCha20Poly1305.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix,
            var _ => throw new ArgumentException($"Unsupported encryption algorithm: {dekAlgorithm}", nameof(dekAlgorithm))
        };

        Assert.Equal(expectedExtension, svc.FileExtension);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void KeyId_IsStored_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var plaintext = "message"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);
        Assert.Equal(keyId, result.KeyId);
        Assert.NotNull(result.KeyVersion);
        Assert.NotEmpty(result.KeyVersion);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_LargeData_Works_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        const string keyId = "test-key";
        // Create a larger payload (100KB)
        var largeData = new byte[100 * 1024];
        new Random(42).NextBytes(largeData);
        var result = svc.Encrypt(largeData, keyId);
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId);
        Assert.Equal(largeData, decrypted);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptDecrypt_MultipleOperations_ProduceDifferentResults_WithDifferentServiceCombinations(EncryptionAlgorithm dekAlgorithm, EncryptionAlgorithm kekAlgorithm)
    {
        var keyStore = CreateKeyStore();
        var dekService = CreateEncryptionService(dekAlgorithm, keyStore);
        var kekService = CreateEncryptionService(kekAlgorithm, keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        const string keyId = "test-key";
        var plaintext = "same message"u8.ToArray();
        var result1 = svc.Encrypt(plaintext, keyId);
        var result2 = svc.Encrypt(plaintext, keyId);

        // Each encryption should produce different encrypted data (due to random DEK)
        Assert.NotEqual(result1.EncryptedData, result2.EncryptedData);
        Assert.NotEqual(result1.EncryptedDataEncryptionKey, result2.EncryptedDataEncryptionKey);

        // But both should decrypt to the same plaintext
        var dec1 = svc.Decrypt(result1.EncryptedData, result1.EncryptedDataEncryptionKey, keyId);
        var dec2 = svc.Decrypt(result2.EncryptedData, result2.EncryptedDataEncryptionKey, keyId);
        Assert.Equal(plaintext, dec1);
        Assert.Equal(plaintext, dec2);
    }
}