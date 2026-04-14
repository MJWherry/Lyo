using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class TwoKeyAesGcmTests
{
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithAesGcmKek()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("secret message");
        var result = svc.Encrypt(plaintext, keyId);
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId);
        Assert.Equal("secret message", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithExternalKek()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "dummy-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Use external KEK passed to methods (this overrides the key store KEK)
        var kekBytes = DeriveKey("external-kek");
        var plaintext = Encoding.UTF8.GetBytes("external kek message");
        var result = svc.Encrypt(plaintext, kek: kekBytes);
        var decrypted = svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, kek: kekBytes);
        Assert.Equal("external kek message", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void EncryptDecrypt_DifferentKekVersions_Isolated()
    {
        const string keyId1 = "test-key-1";
        const string keyId2 = "test-key-2";
        var keyStore1 = new LocalKeyStore();
        keyStore1.UpdateKeyFromString(keyId1, "kek-v1");
        var keyStore2 = new LocalKeyStore();
        keyStore2.UpdateKeyFromString(keyId2, "kek-v2");
        var aesGcmService1 = new AesGcmEncryptionService(keyStore1);
        var aesGcmService2 = new AesGcmEncryptionService(keyStore2);
        using var svc1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService1, keyStore1);
        using var svc2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService2, keyStore2);
        var plaintext = Encoding.UTF8.GetBytes("versioned message");
        var result1 = svc1.Encrypt(plaintext, keyId1);
        var result2 = svc2.Encrypt(plaintext, keyId2);

        // Results should be different due to different KEKs
        Assert.NotEqual(result1.EncryptedData, result2.EncryptedData);
        Assert.NotEqual(result1.EncryptedDataEncryptionKey, result2.EncryptedDataEncryptionKey);
        Assert.Equal(keyId1, result1.KeyId);
        Assert.Equal(keyId2, result2.KeyId);

        // Each should decrypt with its own service
        var dec1 = svc1.Decrypt(result1.EncryptedData, result1.EncryptedDataEncryptionKey, keyId1);
        var dec2 = svc2.Decrypt(result2.EncryptedData, result2.EncryptedDataEncryptionKey, keyId2);
        Assert.Equal("versioned message", Encoding.UTF8.GetString(dec1));
        Assert.Equal("versioned message", Encoding.UTF8.GetString(dec2));
    }

    [Fact]
    public void EncryptDecrypt_WithWrongKek_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "correct-kek");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("message");
        var result = svc.Encrypt(plaintext, keyId);

        // Try to decrypt with wrong KEK
        var wrongKek = DeriveKey("wrong-kek");
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId, wrongKek));
    }

    [Fact]
    public void Encrypt_DecryptString_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        ITwoKeyEncryptionService service = svc;

        // EncryptString now returns TwoKeyEncryptionResult, which can be used with DecryptString
        var result = service.EncryptString("test string", keyId);
        var decrypted = service.DecryptString(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId);
        Assert.Equal("test string", decrypted);
    }

    [Fact]
    public void EncryptString_ReturnsOnlyEncryptedData()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // EncryptString now returns the full TwoKeyEncryptionResult
        ITwoKeyEncryptionService service = svc;
        var result = service.EncryptString("test string", keyId);
        Assert.NotNull(result);
        Assert.NotNull(result.EncryptedData);
        Assert.NotEmpty(result.EncryptedData);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.NotEmpty(result.EncryptedDataEncryptionKey);

        // Verify it's different from another encryption (due to different nonces/DEKs)
        var fullResult = svc.Encrypt(Encoding.UTF8.GetBytes("test string"), keyId);
        Assert.NotEqual(result.EncryptedData, fullResult.EncryptedData); // Should be different due to different nonces
    }

    [Fact]
    public async Task EncryptStreamAsync_DecryptStreamAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var input = new MemoryStream(Encoding.UTF8.GetBytes("This is a long stream content to encrypt"));
        var result = await svc.EncryptStreamAsync(input, keyId).ConfigureAwait(false);
        using var output = new MemoryStream();
        await svc.DecryptStreamAsync(result, output, keyId).ConfigureAwait(false);
        var decrypted = Encoding.UTF8.GetString(output.ToArray());
        Assert.Equal("This is a long stream content to encrypt", decrypted);
    }

    [Fact]
    public async Task Encrypt_EmptyData_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Empty arrays are not allowed
        Assert.Throws<ArgumentException>(() => svc.Encrypt([], keyId));
    }

    [Fact]
    public void Constructor_NoKeyInKeyStore_Throws()
    {
        // Create empty key store (no keys added)
        var emptyKeyStore = new LocalKeyStore();

        // Constructors no longer validate - validation happens when Encrypt is called
        var aesGcmService = new AesGcmEncryptionService(emptyKeyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, emptyKeyStore);

        // Verify that Encrypt throws InvalidOperationException when no key is available
        const string keyId = "test-key";
        var plaintext = Encoding.UTF8.GetBytes("test");
        Assert.Throws<InvalidOperationException>(() => svc.Encrypt(plaintext, keyId));
    }

    [Fact]
    public void FileExtension_IsCorrect()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        Assert.Equal(FileTypeInfo.LyoAesGcm.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix, svc.FileExtension);
    }

    [Fact]
    public void KeyId_IsStored()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("message");
        var result = svc.Encrypt(plaintext, keyId);
        Assert.Equal(keyId, result.KeyId);
        Assert.NotNull(result.KeyVersion);
        Assert.NotEmpty(result.KeyVersion);
    }

    [Fact]
    public void ReEncryptDek_WithKeyRotation_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();

        // Create initial KEK version 1
        var version1 = keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Encrypt data with version 1
        var plaintext = Encoding.UTF8.GetBytes("secret message for rotation");
        var resultV1 = svc.Encrypt(plaintext, keyId);
        Assert.Equal(version1, resultV1.KeyVersion);

        // Verify it decrypts with version 1
        var decryptedV1 = svc.Decrypt(resultV1.EncryptedData, resultV1.EncryptedDataEncryptionKey, keyId, keyVersion: version1);
        Assert.Equal(plaintext, decryptedV1);

        // Rotate to version 2
        var version2 = keyStore.UpdateKeyFromString(keyId, "kek-v2");
        Assert.Equal(version2, keyStore.GetCurrentVersion(keyId));

        // Re-encrypt the DEK with version 2
        var reEncryptedDek = svc.ReEncryptDek(resultV1.EncryptedDataEncryptionKey, keyId, version1);

        // Verify the re-encrypted DEK is different from the original
        Assert.NotEqual(resultV1.EncryptedDataEncryptionKey, reEncryptedDek);

        // Verify the data can be decrypted with the re-encrypted DEK using current (v2) key
        var decryptedV2 = svc.Decrypt(resultV1.EncryptedData, reEncryptedDek, keyId);
        Assert.Equal(plaintext, decryptedV2);

        // Verify it still works with old version 1
        var decryptedV1Again = svc.Decrypt(resultV1.EncryptedData, resultV1.EncryptedDataEncryptionKey, keyId, keyVersion: version1);
        Assert.Equal(plaintext, decryptedV1Again);
    }

    [Fact]
    public async Task ReEncryptDekAsync_WithKeyRotation_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();

        // Create initial KEK version 1
        var version1 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Encrypt data with version 1
        var plaintext = "async rotation test"u8.ToArray();
        var resultV1 = svc.Encrypt(plaintext, keyId);
        Assert.Equal(version1, resultV1.KeyVersion);

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, await keyStore.GetCurrentVersionAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));

        // Re-encrypt the DEK with version 2 (async)
        var reEncryptedDek = await svc.ReEncryptDekAsync(resultV1.EncryptedDataEncryptionKey, keyId, version1, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify the re-encrypted DEK is different from the original
        Assert.NotEqual(resultV1.EncryptedDataEncryptionKey, reEncryptedDek);

        // Verify the data can be decrypted with the re-encrypted DEK using current (v2) key
        var decryptedV2 = svc.Decrypt(resultV1.EncryptedData, reEncryptedDek, keyId);
        Assert.Equal(plaintext, decryptedV2);
    }

    [Fact]
    public void ReEncryptDek_MultipleRotations_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();

        // Create version 1
        var version1 = keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("multi-rotation test");
        var resultV1 = svc.Encrypt(plaintext, keyId);

        // Rotate to version 2
        var version2 = keyStore.UpdateKeyFromString(keyId, "kek-v2");
        var reEncryptedDekV2 = svc.ReEncryptDek(resultV1.EncryptedDataEncryptionKey, keyId, version1);

        // Rotate to version 3
        keyStore.UpdateKeyFromString(keyId, "kek-v3");
        var reEncryptedDekV3 = svc.ReEncryptDek(reEncryptedDekV2, keyId, version2);

        // Verify all versions can decrypt
        var decryptedV1 = svc.Decrypt(resultV1.EncryptedData, resultV1.EncryptedDataEncryptionKey, keyId, keyVersion: version1);
        var decryptedV2 = svc.Decrypt(resultV1.EncryptedData, reEncryptedDekV2, keyId, keyVersion: version2);
        var decryptedV3 = svc.Decrypt(resultV1.EncryptedData, reEncryptedDekV3, keyId);
        Assert.Equal(plaintext, decryptedV1);
        Assert.Equal(plaintext, decryptedV2);
        Assert.Equal(plaintext, decryptedV3);
    }

    [Fact]
    public void ReEncryptDek_OldKeyVersionNotFound_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = "test"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);

        // Try to re-encrypt with non-existent version
        Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, keyId, "999"));
    }

    [Fact]
    public void ReEncryptDek_CurrentKeyNotFound_Throws()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = new LocalKeyStore();

        // Add source key version 1
        keyStore.AddKeyFromString(sourceKeyId, "1", "source-kek-v1");
        keyStore.SetCurrentVersion(sourceKeyId, "1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test");
        var result = svc.Encrypt(plaintext, sourceKeyId);

        // Try to re-encrypt with targetKeyId that doesn't exist (no current key)
        // This should fail because targetKeyId has no current key
        Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, sourceKeyId, "1", targetKeyId));
    }

    [Fact]
    public void ReEncryptDek_SameVersion_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = "test"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);

        // Try to re-encrypt with same version as current
        var currentVersion = keyStore.GetCurrentVersion(keyId);
        var exception = Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, keyId, currentVersion!));
        Assert.Contains("same as the source key version", exception.Message);
    }

    [Fact]
    public void ReEncryptDek_InvalidVersion_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = "test"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);

        // Try to re-encrypt with invalid version (empty or whitespace)
        Assert.Throws<ArgumentException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, keyId, ""));
        Assert.Throws<ArgumentException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, keyId, "   "));

        // Try to re-encrypt with non-existent version
        Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, keyId, "non-existent-version"));
    }

    [Fact]
    public void ReEncryptDek_EmptyKeyId_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = "test"u8.ToArray();
        var result = svc.Encrypt(plaintext, keyId);
        var currentVersion = keyStore.GetCurrentVersion(keyId);
        Assert.ThrowsAny<ArgumentException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, "", currentVersion!));
        Assert.ThrowsAny<ArgumentException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, null!, currentVersion!));
    }

    [Fact]
    public void ReEncryptDek_WithDifferentKeyId_Works()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = new LocalKeyStore();

        // Create source KEK version 1
        var sourceVersion1 = keyStore.UpdateKeyFromString(sourceKeyId, "source-kek-v1");

        // Create target KEK version 1
        keyStore.UpdateKeyFromString(targetKeyId, "target-kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Encrypt data with source key
        var plaintext = Encoding.UTF8.GetBytes("migration test message");
        var resultSource = svc.Encrypt(plaintext, sourceKeyId);
        Assert.Equal(sourceVersion1, resultSource.KeyVersion);
        Assert.Equal(sourceKeyId, resultSource.KeyId);

        // Verify it decrypts with source key
        var decryptedSource = svc.Decrypt(resultSource.EncryptedData, resultSource.EncryptedDataEncryptionKey, sourceKeyId);
        Assert.Equal(plaintext, decryptedSource);

        // Re-encrypt the DEK with target key
        var reEncryptedDek = svc.ReEncryptDek(resultSource.EncryptedDataEncryptionKey, sourceKeyId, sourceVersion1, targetKeyId);

        // Verify the re-encrypted DEK is different from the original
        Assert.NotEqual(resultSource.EncryptedDataEncryptionKey, reEncryptedDek);

        // Verify the data can be decrypted with the re-encrypted DEK using target key
        var decryptedTarget = svc.Decrypt(resultSource.EncryptedData, reEncryptedDek, targetKeyId);
        Assert.Equal(plaintext, decryptedTarget);

        // Verify it still works with original source key (backward compatibility)
        var decryptedSourceAgain = svc.Decrypt(resultSource.EncryptedData, resultSource.EncryptedDataEncryptionKey, sourceKeyId);
        Assert.Equal(plaintext, decryptedSourceAgain);
    }

    [Fact]
    public void ReEncryptDek_WithDifferentKeyIdAndVersion_Works()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = new LocalKeyStore();

        // Create source KEK version 1
        var sourceVersion1 = keyStore.UpdateKeyFromString(sourceKeyId, "source-kek-v1");

        // Create target KEK versions 1 and 2
        keyStore.UpdateKeyFromString(targetKeyId, "target-kek-v1");
        var targetVersion2 = keyStore.UpdateKeyFromString(targetKeyId, "target-kek-v2");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);

        // Encrypt data with source key
        var plaintext = Encoding.UTF8.GetBytes("specific version migration test");
        var resultSource = svc.Encrypt(plaintext, sourceKeyId);

        // Re-encrypt the DEK with target key, specific version 2
        var reEncryptedDek = svc.ReEncryptDek(resultSource.EncryptedDataEncryptionKey, sourceKeyId, sourceVersion1, targetKeyId, targetVersion2);

        // Verify the data can be decrypted with the re-encrypted DEK using target key version 2
        var decryptedTarget = svc.Decrypt(resultSource.EncryptedData, reEncryptedDek, targetKeyId, keyVersion: targetVersion2);
        Assert.Equal(plaintext, decryptedTarget);
    }

    [Fact]
    public void ReEncryptDek_TargetKeyNotFound_Throws()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = new LocalKeyStore();
        var sourceVersion1 = keyStore.UpdateKeyFromString(sourceKeyId, "source-kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test");
        var result = svc.Encrypt(plaintext, sourceKeyId);

        // Try to re-encrypt with non-existent target keyId
        Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, sourceKeyId, sourceVersion1, targetKeyId));
    }

    [Fact]
    public void ReEncryptDek_TargetKeyVersionNotFound_Throws()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = new LocalKeyStore();
        var sourceVersion1 = keyStore.UpdateKeyFromString(sourceKeyId, "source-kek-v1");
        keyStore.UpdateKeyFromString(targetKeyId, "target-kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test");
        var result = svc.Encrypt(plaintext, sourceKeyId);

        // Try to re-encrypt with non-existent target key version
        Assert.Throws<InvalidOperationException>(() => svc.ReEncryptDek(result.EncryptedDataEncryptionKey, sourceKeyId, sourceVersion1, targetKeyId, "999"));
    }

    [Fact]
    public async Task EncryptToStreamAsync_StoresAlgorithmIds()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var expectedVersion = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test data");
        using var inputStream = new MemoryStream(plaintext);
        using var outputStream = new MemoryStream();
        await svc.EncryptToStreamAsync(inputStream, outputStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify stream format: [FormatVersion: 1][DEKAlgorithmId: 1][KEKAlgorithmId: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion][EncryptedDEKLength: 4][EncryptedDEK][Chunks...]
        outputStream.Position = 0;
        using var br = new BinaryReader(outputStream);

        // Read format version
        var formatVersion = br.ReadByte();
        Assert.Equal(1, formatVersion);

        // Read DEK algorithm ID (AES-GCM = 0)
        var dekAlgorithmId = br.ReadByte();
        Assert.Equal(0, dekAlgorithmId);

        // Read KEK algorithm ID (AES-GCM = 0)
        var kekAlgorithmId = br.ReadByte();
        Assert.Equal(0, kekAlgorithmId);

        Assert.Equal(32, br.ReadByte()); // DekKeyMaterialBytes

        // Read keyId length
        var keyIdLength = br.ReadInt32();
        Assert.True(keyIdLength > 0);

        // Read keyId
        var keyIdBytes = br.ReadBytes(keyIdLength);
        var storedKeyId = Encoding.UTF8.GetString(keyIdBytes);
        Assert.Equal(keyId, storedKeyId);

        // Read keyVersion length
        var keyVersionLength = br.ReadInt32();
        Assert.True(keyVersionLength > 0);

        // Read keyVersion
        var keyVersionBytes = br.ReadBytes(keyVersionLength);
        var keyVersion = Encoding.UTF8.GetString(keyVersionBytes);
        Assert.Equal(expectedVersion, keyVersion);
    }

    [Fact]
    public async Task EncryptToStreamAsync_WithDifferentDEKAndKEK_StoresBothAlgorithmIds()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Use ChaCha20Poly1305 for DEK, AES-GCM for KEK
        var dekService = new ChaCha20Poly1305EncryptionService(keyStore);
        var kekService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test data");
        using var inputStream = new MemoryStream(plaintext);
        using var outputStream = new MemoryStream();
        await svc.EncryptToStreamAsync(inputStream, outputStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify stream format
        outputStream.Position = 0;
        using var br = new BinaryReader(outputStream);

        // Read format version
        var formatVersion = br.ReadByte();
        Assert.Equal(1, formatVersion);

        // Read DEK algorithm ID (ChaCha20Poly1305 = 1)
        var dekAlgorithmId = br.ReadByte();
        Assert.Equal(1, dekAlgorithmId);

        // Read KEK algorithm ID (AES-GCM = 0)
        var kekAlgorithmId = br.ReadByte();
        Assert.Equal(0, kekAlgorithmId);

        Assert.Equal(32, br.ReadByte()); // DekKeyMaterialBytes (ChaCha DEK)
    }

    [Fact]
    public async Task DecryptToStreamAsync_ValidatesAlgorithmIds()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test data");
        using var inputStream = new MemoryStream(plaintext);
        using var encryptedStream = new MemoryStream();

        // Encrypt
        await svc.EncryptToStreamAsync(inputStream, encryptedStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Decrypt
        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        await svc.DecryptToStreamAsync(encryptedStream, decryptedStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify decrypted data
        var decrypted = decryptedStream.ToArray();
        Assert.Equal("test data", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public async Task DecryptToStreamAsync_WithWrongDEKAlgorithm_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Encrypt with AES-GCM
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var encryptService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        var plaintext = Encoding.UTF8.GetBytes("test data");
        using var inputStream = new MemoryStream(plaintext);
        using var encryptedStream = new MemoryStream();
        await encryptService.EncryptToStreamAsync(inputStream, encryptedStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Try to decrypt with ChaCha20Poly1305 (wrong DEK algorithm)
        var chachaService = new ChaCha20Poly1305EncryptionService(keyStore);
        using var decryptService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(chachaService, keyStore);
        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();

        // Should throw because DEK magic/algorithm doesn't match
        await Assert.ThrowsAnyAsync<InvalidDataException>(async ()
            => await decryptService.DecryptToStreamAsync(encryptedStream, decryptedStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false));
    }
}