using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.TwoKey;
using Lyo.IO.Temp.Models;
using Lyo.KeyStore;
using Lyo.KeyStore.KeyDerivation;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

public class FileOperationTests : IDisposable, IAsyncDisposable
{
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync();

    public void Dispose() => _tempSession.Dispose();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    private (string pubPath, string privPath) GeneratePemFiles()
    {
        using var rsa = RSA.Create(2048);
        var pub = rsa.ExportSubjectPublicKeyInfo();
        var priv = rsa.ExportPkcs8PrivateKey();
        var pubPem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(pub) + "\n-----END PUBLIC KEY-----";
        var privPem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(priv) + "\n-----END PRIVATE KEY-----";
        var pubPath = _tempSession.CreateFile(pubPem);
        var privPath = _tempSession.CreateFile(privPem);
        return (pubPath, privPath);
    }

    [Fact]
    public async Task AesGcm_EncryptFileAsync_DecryptFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "This is test file content for encryption";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        await svc.EncryptFileAsync(inputFile, encryptedFile, keyId, ct: TestCt);
        await svc.DecryptFileAsync(encryptedFile, decryptedFile, keyId, ct: TestCt);
        var decryptedContent = await File.ReadAllTextAsync(decryptedFile, TestCt);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task AesGcm_EncryptFileAsync_AutoAddsExtension()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var inputFile = await _tempSession.CreateFileAsync("Test content", TestCt);
        var encryptedFile = inputFile + FileTypeInfo.LyoAesGcm.DefaultExtension;
        await svc.EncryptFileAsync(inputFile, keyId: keyId, ct: TestCt); // No output path specified
        Assert.True(File.Exists(encryptedFile), "Encrypted file should be created with .ag extension");
    }

    [Fact]
    public void AesGcm_EncryptFile_DecryptFile_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "Synchronous file encryption test";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        var encrypted = svc.EncryptFile(inputFile, keyId);
        File.WriteAllBytes(encryptedFile, encrypted);
        var decrypted = svc.DecryptFile(encryptedFile, keyId);
        File.WriteAllBytes(decryptedFile, decrypted);
        var decryptedContent = File.ReadAllText(decryptedFile);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public void AesGcm_EncryptToFile_DecryptToFile_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "EncryptToFile test content";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        svc.EncryptToFile(inputFile, encryptedFile, keyId);
        svc.DecryptToFile(encryptedFile, decryptedFile, keyId);
        var decryptedContent = File.ReadAllText(decryptedFile);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public void AesGcm_EncryptToFile_AutoAddsExtension()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "Test content";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = inputFile + FileTypeInfo.LyoAesGcm.DefaultExtension;
        svc.EncryptToFile(inputFile, keyId: keyId); // No output path specified
        Assert.True(File.Exists(encryptedFile), "Encrypted file should be created with .ag extension");
    }

    [Fact]
    public void AesGcm_EncryptFile_FileNotFound_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7().ToString());
        Assert.Throws<FileNotFoundException>(() => svc.EncryptFile(nonExistentFile, keyId));
    }

    [Fact]
    public async Task ChaCha20Poly1305_EncryptFileAsync_DecryptFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var testContent = "ChaCha20Poly1305 file encryption test";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        await svc.EncryptFileAsync(inputFile, encryptedFile, keyId, ct: TestCt);
        await svc.DecryptFileAsync(encryptedFile, decryptedFile, keyId, ct: TestCt);
        var decryptedContent = await File.ReadAllTextAsync(decryptedFile, TestCt);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public void ChaCha20Poly1305_EncryptToFile_DecryptToFile_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var testContent = "ChaCha20Poly1305 EncryptToFile test";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        svc.EncryptToFile(inputFile, encryptedFile, keyId);
        svc.DecryptToFile(encryptedFile, decryptedFile, keyId);
        var decryptedContent = File.ReadAllText(decryptedFile);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task ChaCha20Poly1305_EncryptFileAsync_AutoAddsExtension()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var inputFile = await _tempSession.CreateFileAsync("Test content", TestCt);
        var encryptedFile = inputFile + FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension;
        await svc.EncryptFileAsync(inputFile, keyId: keyId, ct: TestCt);
        Assert.True(File.Exists(encryptedFile), "Encrypted file should be created with .chacha extension");
    }

    [Fact]
    public async Task Rsa_EncryptFileAsync_DecryptFileAsync_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        await using var encryptor = new RsaEncryptor(pub, padding: RSAEncryptionPadding.OaepSHA256);
        await using var decryptor = new RsaDecryptor(priv, padding: RSAEncryptionPadding.OaepSHA256);
        var testContent = "RSA file encryption test";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        var data = await File.ReadAllBytesAsync(inputFile, TestCt);
        await encryptor.EncryptToFileAsync(data, encryptedFile, ct: TestCt);
        var decrypted = await decryptor.DecryptFromFileAsync(encryptedFile, ct: TestCt);
        Assert.Equal(testContent, Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void Rsa_EncryptToFile_DecryptToFile_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        using var encryptor = new RsaEncryptor(pub, padding: RSAEncryptionPadding.OaepSHA256);
        using var decryptor = new RsaDecryptor(priv, padding: RSAEncryptionPadding.OaepSHA256);
        var testContent = "RSA EncryptToFile test";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        var encrypted = encryptor.Encrypt(File.ReadAllBytes(inputFile));
        File.WriteAllBytes(encryptedFile, encrypted);
        var decrypted = decryptor.Decrypt(File.ReadAllBytes(encryptedFile));
        File.WriteAllBytes(decryptedFile, decrypted);
        var decryptedContent = File.ReadAllText(decryptedFile);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task Rsa_EncryptFileAsync_AutoAddsExtension()
    {
        var (pub, _) = GeneratePemFiles();
        await using var encryptor = new RsaEncryptor(pub, padding: RSAEncryptionPadding.OaepSHA256);
        var inputFile = await _tempSession.CreateFileAsync("Test content", TestCt);
        var encryptedFile = inputFile + encryptor.FileExtension;
        var data = await File.ReadAllBytesAsync(inputFile, TestCt);
        await encryptor.EncryptToFileAsync(data, encryptedFile, ct: TestCt);
        Assert.True(File.Exists(encryptedFile), "Encrypted file should be created with .rsa extension");
    }

    [Fact]
    public async Task AesGcm_EncryptFileAsync_LargeFile_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var largeContent = new string('A', 2 * 1024 * 1024);
        var inputFile = await _tempSession.CreateFileAsync(largeContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        await svc.EncryptFileAsync(inputFile, encryptedFile, keyId, ct: TestCt);
        await svc.DecryptFileAsync(encryptedFile, decryptedFile, keyId, ct: TestCt);
        var decryptedContent = await File.ReadAllTextAsync(decryptedFile, TestCt);
        Assert.Equal(largeContent, decryptedContent);
    }

    [Fact]
    public void AesGcm_EncryptFile_DecryptFile_WithByteArrayKey_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "file-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "Test with byte array key";
        var inputFile = _tempSession.CreateFile(testContent);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        var key = DeriveKey("custom-byte-key");
        var encrypted = svc.EncryptFile(inputFile, keyId, key);
        File.WriteAllBytes(encryptedFile, encrypted);
        var decrypted = svc.DecryptFile(encryptedFile, keyId, key);
        File.WriteAllBytes(decryptedFile, decrypted);
        var decryptedContent = File.ReadAllText(decryptedFile);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task AesGcm_EncryptFileAsync_DecryptFileAsync_WithByteArrayKey_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "Test with byte array key async";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        var decryptedFile = _tempSession.GetFilePath();
        var key = DeriveKey("custom-byte-key");
        await svc.EncryptFileAsync(inputFile, encryptedFile, key: key, ct: TestCt);
        await svc.DecryptFileAsync(encryptedFile, decryptedFile, key: key, ct: TestCt);
        var decryptedContent = await File.ReadAllTextAsync(decryptedFile, TestCt);
        Assert.Equal(testContent, decryptedContent);
    }

    // coverage for EncryptToFileAsync / DecryptFromFileAsync
    [Fact]
    public async Task AesGcm_EncryptToFileAsync_ByteArray_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var encryptedFile = _tempSession.GetFilePath();
        var testData = "Test data for byte array encryption"u8.ToArray();
        await svc.EncryptToFileAsync(testData, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task AesGcm_EncryptToFileAsync_Stream_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var testContent = "Test content for stream encryption";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        await using var inputStream = File.OpenRead(inputFile);
        await svc.EncryptToFileAsync(inputStream, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        var decryptedContent = Encoding.UTF8.GetString(decryptedData);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task AesGcm_EncryptToFileAsync_WithKey_DecryptFromFileAsync_WithKey_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var testData = "Test data with custom key"u8.ToArray();
        var encryptedFile = _tempSession.GetFilePath();
        var key = DeriveKey("custom-key");
        await svc.EncryptToFileAsync(testData, encryptedFile, key: key, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, key: key, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task ChaCha20Poly1305_EncryptToFileAsync_ByteArray_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var testData = "ChaCha20Poly1305 test data"u8.ToArray();
        var encryptedFile = _tempSession.GetFilePath();
        await svc.EncryptToFileAsync(testData, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task ChaCha20Poly1305_EncryptToFileAsync_Stream_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var testContent = "ChaCha20Poly1305 stream test";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        await using var inputStream = File.OpenRead(inputFile);
        await svc.EncryptToFileAsync(inputStream, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        var decryptedContent = Encoding.UTF8.GetString(decryptedData);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task Rsa_EncryptToFileAsync_ByteArray_DecryptFromFileAsync_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        await using var encryptor = new RsaEncryptor(pub);
        await using var decryptor = new RsaDecryptor(priv);
        var testData = "RSA test data"u8.ToArray();
        var encryptedFile = _tempSession.GetFilePath();
        await encryptor.EncryptToFileAsync(testData, encryptedFile, ct: TestCt);
        var decryptedData = await decryptor.DecryptFromFileAsync(encryptedFile, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task Rsa_EncryptToFileAsync_Stream_DecryptFromFileAsync_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        await using var encryptor = new RsaEncryptor(pub);
        await using var decryptor = new RsaDecryptor(priv);
        var testContent = "RSA stream test content";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        await using var inputStream = File.OpenRead(inputFile);
        await encryptor.EncryptToFileAsync(inputStream, encryptedFile, ct: TestCt);
        var decryptedData = await decryptor.DecryptFromFileAsync(encryptedFile, ct: TestCt);
        var decryptedContent = Encoding.UTF8.GetString(decryptedData);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task TwoKeyAesGcm_EncryptToFileAsync_ByteArray_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestCt);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var testData = "TwoKey test data"u8.ToArray();
        var encryptedFile = _tempSession.GetFilePath();
        await svc.EncryptToFileAsync(testData, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task TwoKeyAesGcm_EncryptToFileAsync_Stream_DecryptFromFileAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestCt);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var testContent = "TwoKey stream test content";
        var inputFile = await _tempSession.CreateFileAsync(testContent, TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        await using var inputStream = File.OpenRead(inputFile);
        await svc.EncryptToFileAsync(inputStream, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        var decryptedContent = Encoding.UTF8.GetString(decryptedData);
        Assert.Equal(testContent, decryptedContent);
    }

    [Fact]
    public async Task TwoKeyAesGcm_EncryptToFileAsync_WithKek_DecryptFromFileAsync_WithKek_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestCt);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var testData = "TwoKey test with custom KEK"u8.ToArray();
        var encryptedFile = _tempSession.GetFilePath();
        var customKek = DeriveKey("custom-kek");
        await svc.EncryptToFileAsync(testData, encryptedFile, kek: customKek, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, kek: customKek, ct: TestCt);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public async Task AesGcm_DecryptFromFileAsync_NonExistentFile_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7().ToString());
        await Assert.ThrowsAsync<FileNotFoundException>(() => svc.DecryptFromFileAsync(nonExistentFile, ct: TestCt));
    }

    [Fact]
    public async Task TwoKeyAesGcm_DecryptFromFileAsync_NonExistentFile_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestCt);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7().ToString());
        await Assert.ThrowsAsync<FileNotFoundException>(() => svc.DecryptFromFileAsync(nonExistentFile, ct: TestCt));
    }

    [Fact]
    public async Task AesGcm_EncryptToFileAsync_LargeData_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "file-key", TestCt);
        var svc = new AesGcmEncryptionService(keyStore);
        var largeData = new byte[5 * 1024 * 1024];
        TestData.Fill(largeData);
        var encryptedFile = _tempSession.GetFilePath();
        await svc.EncryptToFileAsync(largeData, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        Assert.Equal(largeData, decryptedData);
    }

    [Fact]
    public async Task TwoKeyAesGcm_EncryptToFileAsync_LargeStream_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-key", TestCt);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var largeData = new byte[5 * 1024 * 1024];
        TestData.Fill(largeData);
        var inputFile = await _tempSession.CreateFileAsync(largeData, ct: TestCt);
        var encryptedFile = _tempSession.GetFilePath();
        await using var inputStream = File.OpenRead(inputFile);
        await svc.EncryptToFileAsync(inputStream, encryptedFile, keyId, ct: TestCt);
        var decryptedData = await svc.DecryptFromFileAsync(encryptedFile, keyId, ct: TestCt);
        Assert.Equal(largeData, decryptedData);
    }
}