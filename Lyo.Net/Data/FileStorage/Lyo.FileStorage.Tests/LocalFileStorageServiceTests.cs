using System.Text;
using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.Keystore;
using Lyo.Testing;
using Microsoft.Extensions.Logging;
using LocalFileStorageServiceOptions = Lyo.FileStorage.Models.LocalFileStorageServiceOptions;

namespace Lyo.FileStorage.Tests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly IIOTempSession _tempSession;

    public LocalFileStorageServiceTests(ITestOutputHelper output)
    {
        // Create logger factory that writes to test output
        _loggerFactory = LoggerFactory.Create(builder => {
            //builder.AddConsole();
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), _loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _tempSession.Dispose();
    }

    private LocalFileStorageService CreateService(
        bool enableDuplicateDetection = false,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? encryptionService = null,
        bool throwOnFileNotFound = true,
        bool throwOnDeleteNotFound = true,
        HashAlgorithm? hashAlgorithm = null)
    {
        var options = new LocalFileStorageServiceOptions {
            RootDirectoryPath = _tempSession.SessionDirectory,
            EnableDuplicateDetection = enableDuplicateDetection,
            ThrowOnFileNotFound = throwOnFileNotFound,
            ThrowOnDeleteNotFound = throwOnDeleteNotFound
        };

        if (hashAlgorithm.HasValue)
            options.HashAlgorithm = hashAlgorithm.Value;

        return new(options, _loggerFactory, compressionService, encryptionService);
    }

    private LocalKeyStore CreateKeyStoreWithKey(string keyId = "test-key", string version = "1", string keyString = "test-kek-key")
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, version, keyString);
        keyStore.SetCurrentVersion(keyId, version);
        return keyStore;
    }

    [Fact]
    public async Task SaveFileAsync_Basic_SavesFileSuccessfully()
    {
        using var service = CreateService();
        var testData = "Hello, World!"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test.txt", result.OriginalFileName);
        Assert.Equal(testData.Length, result.OriginalFileSize);
        Assert.False(result.IsCompressed);
        Assert.False(result.IsEncrypted);
        Assert.NotNull(result.OriginalFileHash);
        Assert.NotNull(result.SourceFileHash);
        Assert.True(File.Exists(Path.Combine(_tempSession.SessionDirectory, GetSubPath(result.Id, ""))));
    }

    [Fact]
    public async Task GetFileAsync_Basic_RetrievesFileSuccessfully()
    {
        using var service = CreateService();
        var testData = "Test content for retrieval"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, "retrieve.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task GetMetadataAsync_Basic_RetrievesMetadataSuccessfully()
    {
        using var service = CreateService();
        var testData = "Metadata test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, "meta.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata = await service.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(metadata);
        Assert.Equal(saveResult.Id, metadata.Id);
        Assert.Equal("meta.txt", metadata.OriginalFileName);
        Assert.Equal(testData.Length, metadata.OriginalFileSize);
    }

    [Fact]
    public async Task DeleteFileAsync_Basic_DeletesFileSuccessfully()
    {
        using var service = CreateService();
        var testData = "Delete me"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var deleted = await service.DeleteFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(deleted);
        var filePath = Path.Combine(_tempSession.SessionDirectory, GetSubPath(saveResult.Id, ""));
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveFileAsync_WithCompression_CompressesFile()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('A', 1000) + "Compress me!" + new string('B', 1000));
        var result = await service.SaveFileAsync(testData, "compressed.txt", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.NotNull(result.CompressionAlgorithm);
        Assert.NotNull(result.CompressedFileSize);
        Assert.True(result.CompressedFileSize < result.OriginalFileSize);
        Assert.NotNull(result.CompressedFileHash);
    }

    [Fact]
    public async Task GetFileAsync_WithCompression_DecompressesFile()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('X', 1000) + "Decompress me!" + new string('Y', 1000));
        var saveResult = await service.SaveFileAsync(testData, "compressed.txt", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_WithEncryption_EncryptsFile()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = "Encrypt this secret message"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.EncryptedFileSize);
        Assert.NotNull(result.EncryptedFileHash);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.Equal(keyId, result.DataEncryptionKeyId);
        Assert.NotNull(result.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task GetFileAsync_WithEncryption_DecryptsFile()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = "Decrypt this secret"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_WithCompressionAndEncryption_ProcessesBoth()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressionService = new CompressionService();
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(compressionService: compressionService, encryptionService: encryptionService);
        var testData = Encoding.UTF8.GetBytes(new string('Z', 1000) + "Compress and encrypt!" + new string('W', 1000));
        var result = await service.SaveFileAsync(testData, "both.txt", true, true, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.CompressionAlgorithm);
        Assert.NotNull(result.CompressedFileSize);
        Assert.NotNull(result.EncryptedFileSize);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.Equal(keyId, result.DataEncryptionKeyId);
    }

    [Fact]
    public async Task GetFileAsync_WithCompressionAndEncryption_DecompressesAndDecrypts()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressionService = new CompressionService();
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(compressionService: compressionService, encryptionService: encryptionService);
        var testData = Encoding.UTF8.GetBytes(new string('M', 1000) + "Round trip test!" + new string('N', 1000));
        var saveResult = await service.SaveFileAsync(testData, "both.txt", true, true, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_NullData_ThrowsArgumentException()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveFileAsync((byte[])null!, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_EmptyData_ThrowsArgumentException()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveFileAsync([], ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_CompressWithoutService_ThrowsInvalidOperationException()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveFileAsync("test"u8.ToArray(), compress: true, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_EncryptWithoutService_ThrowsInvalidOperationException()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveFileAsync("test"u8.ToArray(), encrypt: true, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_EncryptWithoutKeyId_ThrowsInvalidOperationException()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveFileAsync("test"u8.ToArray(), encrypt: true, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_MultiTenant_WithDifferentKeyIds_Works()
    {
        const string keyId1 = "client-a";
        const string keyId2 = "client-b";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId1, "1", "client-a-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId2, "1", "client-b-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId1, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId2, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var clientAData = "Client A secret data"u8.ToArray();
        var clientBData = "Client B secret data"u8.ToArray();

        // Encrypt with client A's key
        var resultA = await service.SaveFileAsync(clientAData, "client-a-file.txt", encrypt: true, keyId: keyId1, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(resultA.IsEncrypted);
        Assert.Equal(keyId1, resultA.DataEncryptionKeyId);

        // Encrypt with client B's key
        var resultB = await service.SaveFileAsync(clientBData, "client-b-file.txt", encrypt: true, keyId: keyId2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(resultB.IsEncrypted);
        Assert.Equal(keyId2, resultB.DataEncryptionKeyId);

        // Verify both can be decrypted correctly
        var decryptedA = await service.GetFileAsync(resultA.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decryptedB = await service.GetFileAsync(resultB.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(clientAData, decryptedA);
        Assert.Equal(clientBData, decryptedB);

        // Verify metadata stores correct keyId
        var metadataA = await service.GetMetadataAsync(resultA.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadataB = await service.GetMetadataAsync(resultB.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId1, metadataA.DataEncryptionKeyId);
        Assert.Equal(keyId2, metadataB.DataEncryptionKeyId);
    }

    [Fact]
    public async Task GetFileAsync_NonExistentFile_ThrowsFileNotFoundException_ByDefault()
    {
        using var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetFileAsync(nonExistentId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetMetadataAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        using var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetMetadataAsync(nonExistentId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task FileSaved_Event_RaisesOnSave()
    {
        using var service = CreateService();
        FileSavedResult? eventArgs = null;
        service.FileSaved += (_, args) => eventArgs = args;
        var testData = "Event test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "event.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.Equal(result.Id, eventArgs.FileId);
        Assert.Equal(testData.Length, eventArgs.OriginalSize);
        Assert.False(eventArgs.WasCompressed);
        Assert.False(eventArgs.WasEncrypted);
    }

    [Fact]
    public async Task FileRetrieved_Event_RaisesOnRetrieve()
    {
        using var service = CreateService();
        FileRetrievedResult? eventArgs = null;
        service.FileRetrieved += (_, args) => eventArgs = args;
        var testData = "Retrieve event test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.Equal(saveResult.Id, eventArgs.FileId);
        Assert.Equal(testData.Length, eventArgs.FileSize);
    }

    [Fact]
    public async Task FileDeleted_Event_RaisesOnDelete()
    {
        using var service = CreateService();
        FileDeletedResult? eventArgs = null;
        service.FileDeleted += (_, args) => eventArgs = args;
        var testData = "Delete event test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.DeleteFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.Equal(saveResult.Id, eventArgs.FileId);
        Assert.True(eventArgs.Success);
    }

    [Fact]
    public async Task FileMetadataRetrieved_Event_RaisesOnGetMetadata()
    {
        using var service = CreateService();
        FileMetadataRetrievedResult? eventArgs = null;
        service.FileMetadataRetrieved += (_, args) => eventArgs = args;
        var testData = "Metadata event test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.Equal(saveResult.Id, eventArgs.FileId);
        Assert.NotNull(eventArgs.FileStoreResult);
    }

    [Fact]
    public async Task SaveFileAsync_WithoutOriginalFileName_UsesFileId()
    {
        using var service = CreateService();
        var testData = "No filename test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result.OriginalFileName);
        Assert.Equal(result.Id.ToString(), result.OriginalFileName);
    }

    [Fact]
    public async Task GetFileAsync_CompressedWithoutService_ThrowsInvalidOperationException()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('A', 1000) + "Compress test");
        var saveResult = await service.SaveFileAsync(testData, compress: true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create a new service without compression service
        using var serviceWithoutCompression = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceWithoutCompression.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetFileAsync_EncryptedWithoutService_ThrowsInvalidOperationException()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = "Encrypt test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create a new service without encryption service
        using var serviceWithoutEncryption = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceWithoutEncryption.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ThrowsFileNotFoundException_ByDefault()
    {
        using var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.DeleteFileAsync(nonExistentId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse_WhenThrowOnDeleteNotFoundIsFalse()
    {
        using var service = CreateService(throwOnDeleteNotFound: false);
        var nonExistentId = Guid.NewGuid();
        var result = await service.DeleteFileAsync(nonExistentId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result);
    }

    [Fact]
    public async Task GetFileAsync_NonExistentFile_ThrowsFileNotFoundException_WhenThrowOnFileNotFoundIsTrue()
    {
        using var service = CreateService(throwOnFileNotFound: true);
        var nonExistentId = Guid.NewGuid();
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetFileAsync(nonExistentId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetFileAsync_NonExistentFile_ReturnsEmptyArray_WhenThrowOnFileNotFoundIsFalse()
    {
        using var service = CreateService(throwOnFileNotFound: false);
        var nonExistentId = Guid.NewGuid();
        var result = await service.GetFileAsync(nonExistentId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue_WhenThrowOnDeleteNotFoundIsFalse()
    {
        using var service = CreateService(throwOnDeleteNotFound: false);
        var testData = "Test file for delete"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.DeleteFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result);
    }

    [Fact]
    public async Task GetFileAsync_ExistingFile_ReturnsData_WhenThrowOnFileNotFoundIsFalse()
    {
        using var service = CreateService(throwOnFileNotFound: false);
        var testData = "Test file for get"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task SaveFileAsync_MultipleFiles_StoresInSubdirectories()
    {
        using var service = CreateService();
        var testData1 = "File 1"u8.ToArray();
        var testData2 = "File 2"u8.ToArray();
        var result1 = await service.SaveFileAsync(testData1, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result2 = await service.SaveFileAsync(testData2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var filePath1 = Path.Combine(_tempSession.SessionDirectory, GetSubPath(result1.Id, ""));
        var filePath2 = Path.Combine(_tempSession.SessionDirectory, GetSubPath(result2.Id, ""));
        Assert.True(File.Exists(filePath1));
        Assert.True(File.Exists(filePath2));

        // Files should be in subdirectories based on GUID
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), filePath1);
        Assert.True(filePath2 != null && filePath2.Contains(Path.DirectorySeparatorChar.ToString()));
    }

    [Fact]
    public async Task GetFileAsync_HashMismatch_LogsWarning()
    {
        using var service = CreateService();
        var testData = "Hash test"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Manually corrupt the file
        var filePath = Path.Combine(_tempSession.SessionDirectory, GetSubPath(saveResult.Id, ""));
        var corruptedData = new byte[] { 0xFF, 0xFF, 0xFF };
        await File.WriteAllBytesAsync(filePath, corruptedData, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Should still retrieve (hash mismatch only logs warning, doesn't throw)
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // The retrieved data will be the corrupted data
        Assert.Equal(corruptedData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_LargeFile_HandlesSuccessfully()
    {
        using var service = CreateService();
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var result = await service.SaveFileAsync(largeData, "large.bin", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(largeData.Length, result.OriginalFileSize);
        var retrievedData = await service.GetFileAsync(result.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(largeData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_WithCompression_LargeFile_CompressesWell()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        // Create data that compresses well (repeating patterns)
        var largeData = new byte[1024 * 100]; // 100KB
        for (var i = 0; i < largeData.Length; i++)
            largeData[i] = (byte)(i % 256);

        var result = await service.SaveFileAsync(largeData, "large-compressed.bin", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.NotNull(result.CompressedFileSize);
        Assert.True(result.CompressedFileSize < result.OriginalFileSize);
        var retrievedData = await service.GetFileAsync(result.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(largeData, retrievedData);
    }

    [Fact]
    public async Task EncryptionKeyRotation_ReEncryptsWithNewKeyVersion_Successfully()
    {
        const string keyId = "test-key";
        // Setup key store with both key versions
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "test-kek-key-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "test-kek-key-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Setup encryption service with key version 1
        var aesGcmService1 = new AesGcmEncryptionService(keyStore);
        var encryptionService1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService1, keyStore);
        using var service1 = CreateService(encryptionService: encryptionService1);
        var originalData = "Secret data that needs key rotation"u8.ToArray();

        // Step 1: Encrypt file with key version 1
        var saveResult1 = await service1.SaveFileAsync(originalData, "secret.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(saveResult1.IsEncrypted);
        Assert.Equal(keyId, saveResult1.DataEncryptionKeyId);
        Assert.Equal("1", saveResult1.DataEncryptionKeyVersion);
        Assert.NotNull(saveResult1.EncryptedDataEncryptionKey);

        // Step 2: Verify we can decrypt with version 1
        var decryptedData1 = await service1.GetFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedData1);

        // Step 3: Rotate to key version 2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService2 = new AesGcmEncryptionService(keyStore);
        var encryptionService2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService2, keyStore);
        using var service2 = CreateService(encryptionService: encryptionService2);

        // Step 4: Re-encrypt the file with version 2
        // First decrypt with version 1, then encrypt with version 2
        var decryptedData = await service1.GetFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Delete the old file
        await service1.DeleteFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Save with version 2
        var saveResult2 = await service2.SaveFileAsync(decryptedData, "secret.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(saveResult2.IsEncrypted);
        Assert.Equal(keyId, saveResult2.DataEncryptionKeyId);
        Assert.Equal("2", saveResult2.DataEncryptionKeyVersion);
        Assert.NotNull(saveResult2.EncryptedDataEncryptionKey);

        // Step 5: Verify we can decrypt with version 2
        var decryptedData2 = await service2.GetFileAsync(saveResult2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedData2);

        // Step 6: Verify version 1 service can still decrypt version 1 files using key store
        // Create a new file with version 1 to test
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult1New = await service1.SaveFileAsync(originalData, "secret-v1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResult1New.DataEncryptionKeyId);
        Assert.Equal("1", saveResult1New.DataEncryptionKeyVersion);
        var decryptedV1 = await service1.GetFileAsync(saveResult1New.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1);
    }

    [Fact]
    public async Task EncryptionKeyRotation_WithKeyStore_CanDecryptOldVersions()
    {
        // This test demonstrates that with a key store, we can decrypt files encrypted with old key versions
        const string keyId = "test-key";
        var kekKey = "test-kek-key-rotation";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", kekKey, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", kekKey, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Setup encryption service with key version 1
        var aesGcmService1 = new AesGcmEncryptionService(keyStore);
        var encryptionService1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService1, keyStore);
        using var service1 = CreateService(encryptionService: encryptionService1);
        var originalData = "Data encrypted with version 1"u8.ToArray();

        // Encrypt with version 1
        var saveResult1 = await service1.SaveFileAsync(originalData, "v1-encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResult1.DataEncryptionKeyId);
        Assert.Equal("1", saveResult1.DataEncryptionKeyVersion);

        // Decrypt with version 1
        var decrypted1 = await service1.GetFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decrypted1);

        // Rotate to version 2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService2 = new AesGcmEncryptionService(keyStore);
        var encryptionService2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService2, keyStore);
        using var service2 = CreateService(encryptionService: encryptionService2);

        // Encrypt new file with version 2
        var saveResult2 = await service2.SaveFileAsync(originalData, "v2-encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResult2.DataEncryptionKeyId);
        Assert.Equal("2", saveResult2.DataEncryptionKeyVersion);

        // Decrypt version 2 file with version 2 service
        var decrypted2 = await service2.GetFileAsync(saveResult2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decrypted2);

        // Key test: Decrypt version 1 file with version 2 service (using key store)
        // The key store allows us to retrieve the version 1 key even though current version is 2
        var decryptedV1WithV2 = await service2.GetFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1WithV2);

        // Also verify version 1 service can decrypt version 2 files (same KEK, different version)
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decryptedV2WithV1 = await service1.GetFileAsync(saveResult2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV2WithV1);
    }

    [Fact]
    public async Task EncryptionKeyRotation_GetKeyByVersion_RetrievesCorrectKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "key-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "key-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "3", "key-v3", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify we can get keys by version
        Assert.True(await keyStore.HasKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.True(await keyStore.HasKeyAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.True(await keyStore.HasKeyAsync(keyId, "3", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.False(await keyStore.HasKeyAsync(keyId, "4", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.NotNull(await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.NotNull(await keyStore.GetKeyAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.NotNull(await keyStore.GetKeyAsync(keyId, "3", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Null(await keyStore.GetKeyAsync(keyId, "4", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal("2", await keyStore.GetCurrentVersionAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.NotNull(await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));

        // Verify current key matches version 2
        var v2Key = await keyStore.GetKeyAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var currentKey = await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(v2Key, currentKey);
    }

    [Fact]
    public async Task EncryptWithV1_DecryptWithV2Service_UsesV1Key()
    {
        // Arrange: Setup keystore with both v1 and v2 keys
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 1: Encrypt file with keystore target v1
        var aesGcmServiceV1 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV1, keyStore);
        using var storageServiceV1 = CreateService(encryptionService: encryptionServiceV1);
        var originalData = "Secret data encrypted with v1"u8.ToArray();
        var saveResult = await storageServiceV1.SaveFileAsync(originalData, "secret.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify file was encrypted with v1
        Assert.True(saveResult.IsEncrypted);
        Assert.Equal(keyId, saveResult.DataEncryptionKeyId);
        Assert.Equal("1", saveResult.DataEncryptionKeyVersion);
        Assert.NotNull(saveResult.EncryptedDataEncryptionKey);

        // Step 2: Create new storage service, but target v2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmServiceV2 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV2, keyStore);
        using var storageServiceV2 = CreateService(encryptionService: encryptionServiceV2);

        // Verify current version is v2
        Assert.Equal("2", await keyStore.GetCurrentVersionAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));

        // Step 3: Get file above which should be v1 key even though current v is v2
        var retrievedData = await storageServiceV2.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 4: File should be decrypted successfully and correct key used
        Assert.Equal(originalData, retrievedData);
        Assert.Equal("Secret data encrypted with v1", Encoding.UTF8.GetString(retrievedData));

        // Verify the metadata still shows v1
        var metadata = await storageServiceV2.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", metadata.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task EncryptWithV1_DecryptWithV2Service_KeystoreHasBothVersions()
    {
        // Arrange: Setup keystore with both v1 and v2 keys (different passwords)
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "v1-secret-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "v2-different-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 1: Encrypt file with keystore target v1
        var aesGcmServiceV1 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV1, keyStore);
        using var storageServiceV1 = CreateService(encryptionService: encryptionServiceV1);
        var originalData = "Data encrypted with version 1 key"u8.ToArray();
        var saveResult = await storageServiceV1.SaveFileAsync(originalData, "v1-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResult.DataEncryptionKeyId);
        Assert.Equal("1", saveResult.DataEncryptionKeyVersion);

        // Verify v1 key is different from v2 key
        var v1Key = await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2Key = await keyStore.GetKeyAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(v1Key);
        Assert.NotNull(v2Key);
        Assert.NotEqual(v1Key, v2Key);

        // Step 2: Create new storage service, but target v2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmServiceV2 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV2, keyStore);

        // Verify current version is v2
        Assert.Equal(v2Key, await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));
        using var storageServiceV2 = CreateService(encryptionService: encryptionServiceV2);

        // Step 3: Get file above which should be v1 key even though current v is v2
        var retrievedData = await storageServiceV2.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 4: File should be decrypted successfully and correct key (v1) used
        Assert.Equal(originalData, retrievedData);

        // Verify that v1 key was used (not v2 key)
        // If v2 key was used, decryption would fail or produce wrong data
        var metadata = await storageServiceV2.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", metadata.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task EncryptWithV1_DecryptWithV2Service_MultipleFiles()
    {
        // Arrange: Setup keystore with both v1 and v2 keys
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 1: Encrypt multiple files with v1
        var aesGcmServiceV1 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV1, keyStore);
        using var storageServiceV1 = CreateService(encryptionService: encryptionServiceV1);
        var data1 = "File 1 encrypted with v1"u8.ToArray();
        var data2 = "File 2 encrypted with v1"u8.ToArray();
        var data3 = "File 3 encrypted with v1"u8.ToArray();
        var saveResult1 = await storageServiceV1.SaveFileAsync(data1, "file1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult2 = await storageServiceV1.SaveFileAsync(data2, "file2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult3 = await storageServiceV1.SaveFileAsync(data3, "file3.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.All([saveResult1, saveResult2, saveResult3], result => Assert.Equal("1", result.DataEncryptionKeyVersion));

        // Step 2: Create new storage service, but target v2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmServiceV2 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV2, keyStore);
        using var storageServiceV2 = CreateService(encryptionService: encryptionServiceV2);

        // Step 3: Get all files - they should all use v1 key even though current v is v2
        var retrieved1 = await storageServiceV2.GetFileAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved2 = await storageServiceV2.GetFileAsync(saveResult2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved3 = await storageServiceV2.GetFileAsync(saveResult3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 4: All files should be decrypted successfully with correct keys
        Assert.Equal(data1, retrieved1);
        Assert.Equal(data2, retrieved2);
        Assert.Equal(data3, retrieved3);

        // Verify metadata shows v1 for all
        var metadata1 = await storageServiceV2.GetMetadataAsync(saveResult1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata2 = await storageServiceV2.GetMetadataAsync(saveResult2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata3 = await storageServiceV2.GetMetadataAsync(saveResult3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", metadata1.DataEncryptionKeyVersion);
        Assert.Equal("1", metadata2.DataEncryptionKeyVersion);
        Assert.Equal("1", metadata3.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task EncryptWithV1_DecryptWithV2Service_ThenEncryptNewFileWithV2()
    {
        // Arrange: Setup keystore with both v1 and v2 keys
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 1: Encrypt file with keystore target v1
        var aesGcmServiceV1 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV1, keyStore);
        using var storageServiceV1 = CreateService(encryptionService: encryptionServiceV1);
        var v1Data = "Data encrypted with v1"u8.ToArray();
        var v1SaveResult = await storageServiceV1.SaveFileAsync(v1Data, "v1-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, v1SaveResult.DataEncryptionKeyId);
        Assert.Equal("1", v1SaveResult.DataEncryptionKeyVersion);

        // Step 2: Create new storage service, but target v2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmServiceV2 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV2, keyStore);
        using var storageServiceV2 = CreateService(encryptionService: encryptionServiceV2);

        // Step 3: Get v1 file - should use v1 key
        var retrievedV1 = await storageServiceV2.GetFileAsync(v1SaveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(v1Data, retrievedV1);

        // Step 4: Encrypt new file with v2 (current version)
        var v2Data = "Data encrypted with v2"u8.ToArray();
        var v2SaveResult = await storageServiceV2.SaveFileAsync(v2Data, "v2-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, v2SaveResult.DataEncryptionKeyId);
        Assert.Equal("2", v2SaveResult.DataEncryptionKeyVersion);

        // Step 5: Verify both files can be decrypted correctly
        var retrievedV1Again = await storageServiceV2.GetFileAsync(v1SaveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedV2 = await storageServiceV2.GetFileAsync(v2SaveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(v1Data, retrievedV1Again);
        Assert.Equal(v2Data, retrievedV2);

        // Verify metadata shows correct versions
        var v1Metadata = await storageServiceV2.GetMetadataAsync(v1SaveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2Metadata = await storageServiceV2.GetMetadataAsync(v2SaveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", v1Metadata.DataEncryptionKeyVersion);
        Assert.Equal("2", v2Metadata.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task EncryptWithV1_DecryptWithV2Service_KeyStoreRetrievesCorrectVersion()
    {
        // Arrange: Setup keystore with both v1 and v2 keys
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "v1-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "v2-password", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Store v1 key for later verification
        var v1Key = await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2Key = await keyStore.GetKeyAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 1: Encrypt file with keystore target v1
        var aesGcmServiceV1 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV1 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV1, keyStore);
        using var storageServiceV1 = CreateService(encryptionService: encryptionServiceV1);
        var originalData = "Test data for key version verification"u8.ToArray();
        var saveResult = await storageServiceV1.SaveFileAsync(originalData, "test.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResult.DataEncryptionKeyId);
        Assert.Equal("1", saveResult.DataEncryptionKeyVersion);

        // Step 2: Create new storage service, but target v2
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmServiceV2 = new AesGcmEncryptionService(keyStore);
        var encryptionServiceV2 = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmServiceV2, keyStore);
        using var storageServiceV2 = CreateService(encryptionService: encryptionServiceV2);

        // Verify current key is v2
        Assert.Equal(v2Key, await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.NotEqual(v1Key, await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));

        // Step 3: Get file - should use v1 key from keystore
        var retrievedData = await storageServiceV2.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Step 4: File should be decrypted successfully using v1 key
        Assert.Equal(originalData, retrievedData);

        // Verify that keystore can still retrieve v1 key by version
        var retrievedV1Key = await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(v1Key, retrievedV1Key);
        Assert.NotEqual(v2Key, retrievedV1Key);
    }

    private IEncryptionService CreateEncryptionService(string serviceType, IKeyStore keyStore)
        => serviceType switch {
            "AesGcm" => new AesGcmEncryptionService(keyStore),
            "ChaCha20Poly1305" => new ChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new ArgumentException($"Unknown service type: {serviceType}", nameof(serviceType))
        };

    [Theory]
    [InlineData("AesGcm", "AesGcm")]
    [InlineData("AesGcm", "ChaCha20Poly1305")]
    [InlineData("ChaCha20Poly1305", "AesGcm")]
    [InlineData("ChaCha20Poly1305", "ChaCha20Poly1305")]
    public async Task SaveFileAsync_WithMixedEncryptionServices_EncryptsSuccessfully(string dekServiceType, string kekServiceType)
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var expectedVersion = await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dekService = CreateEncryptionService(dekServiceType, keyStore);
        var kekService = CreateEncryptionService(kekServiceType, keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = Encoding.UTF8.GetBytes($"Secret message with {dekServiceType} DEK and {kekServiceType} KEK");
        var result = await service.SaveFileAsync(testData, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.EncryptedFileSize);
        Assert.NotNull(result.EncryptedFileHash);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.NotNull(result.DataEncryptionKeyVersion);
        Assert.Equal(expectedVersion, result.DataEncryptionKeyVersion);
    }

    [Theory]
    [InlineData("AesGcm", "AesGcm")]
    [InlineData("AesGcm", "ChaCha20Poly1305")]
    [InlineData("ChaCha20Poly1305", "AesGcm")]
    [InlineData("ChaCha20Poly1305", "ChaCha20Poly1305")]
    public async Task GetFileAsync_WithMixedEncryptionServices_DecryptsSuccessfully(string dekServiceType, string kekServiceType)
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dekService = CreateEncryptionService(dekServiceType, keyStore);
        var kekService = CreateEncryptionService(kekServiceType, keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = Encoding.UTF8.GetBytes($"Decrypt test with {dekServiceType} DEK and {kekServiceType} KEK");
        var saveResult = await service.SaveFileAsync(testData, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Theory]
    [InlineData("AesGcm", "AesGcm")]
    [InlineData("AesGcm", "ChaCha20Poly1305")]
    [InlineData("ChaCha20Poly1305", "AesGcm")]
    [InlineData("ChaCha20Poly1305", "ChaCha20Poly1305")]
    public async Task SaveFileAsync_AddNewKeyAndUpdateVersion_WorksWithDifferentKekBytes(string dekType, string kekType)
    {
        // Note: Both DEK and KEK services must stay the same - only KEK bytes (key version) can change
        // Setup key store with version 1
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create service with encryption combo
        var dekService = CreateEncryptionService(dekType, keyStore);
        var kekService = CreateEncryptionService(kekType, keyStore);
        var v1EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV1 = CreateService(encryptionService: v1EncryptionService);
        var originalData = Encoding.UTF8.GetBytes($"Data encrypted with v1 ({dekType} DEK, {kekType} KEK)");
        var saveResultV1 = await serviceV1.SaveFileAsync(originalData, "v1-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(saveResultV1.IsEncrypted);
        Assert.Equal(keyId, saveResultV1.DataEncryptionKeyId);
        Assert.Equal("1", saveResultV1.DataEncryptionKeyVersion);

        // Verify we can decrypt with v1
        var decryptedV1 = await serviceV1.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1);

        // Add new key version 2 (different KEK bytes, same KEK service type)
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create service with v2 (same services, different KEK bytes)
        var v2EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV2 = CreateService(encryptionService: v2EncryptionService);

        // Save new file with v2
        var newData = Encoding.UTF8.GetBytes($"Data encrypted with v2 ({dekType} DEK, {kekType} KEK)");
        var saveResultV2 = await serviceV2.SaveFileAsync(newData, "v2-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(saveResultV2.IsEncrypted);
        Assert.Equal(keyId, saveResultV2.DataEncryptionKeyId);
        Assert.Equal("2", saveResultV2.DataEncryptionKeyVersion);

        // Verify both files can be decrypted with their respective services
        var decryptedV1Again = await serviceV1.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decryptedV2 = await serviceV2.GetFileAsync(saveResultV2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1Again);
        Assert.Equal(newData, decryptedV2);

        // Verify v2 service can decrypt v1 file (using key store, same services)
        var decryptedV1WithV2 = await serviceV2.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1WithV2);
    }

    [Theory]
    [InlineData("AesGcm", "AesGcm")]
    [InlineData("ChaCha20Poly1305", "ChaCha20Poly1305")]
    public async Task GetFileAsync_WithOlderKeyVersion_DecryptsSuccessfully(string dekType, string kekType)
    {
        // Note: Both DEK and KEK services must stay the same - only KEK bytes (key version) can change
        // Setup key store with both versions (different KEK bytes, same KEK service type)
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create service with v1 encryption combo
        var dekService = CreateEncryptionService(dekType, keyStore);
        var kekService = CreateEncryptionService(kekType, keyStore);
        var v1EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV1 = CreateService(encryptionService: v1EncryptionService);

        // Encrypt file with v1
        var originalData = Encoding.UTF8.GetBytes($"Old data encrypted with v1 ({dekType} DEK, {kekType} KEK)");
        var saveResultV1 = await serviceV1.SaveFileAsync(originalData, "old-file.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResultV1.DataEncryptionKeyId);
        Assert.Equal("1", saveResultV1.DataEncryptionKeyVersion);

        // Switch to v2 with different KEK bytes (but same KEK service type)
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV2 = CreateService(encryptionService: v2EncryptionService);

        // Verify v2 service can decrypt v1 file (using key store to get v1 key, same services)
        var decryptedWithV2 = await serviceV2.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedWithV2);

        // Verify metadata shows v1
        var metadata = await serviceV2.GetMetadataAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", metadata.DataEncryptionKeyVersion);
    }

    [Theory]
    [InlineData("AesGcm", "AesGcm")]
    [InlineData("AesGcm", "ChaCha20Poly1305")]
    [InlineData("ChaCha20Poly1305", "AesGcm")]
    [InlineData("ChaCha20Poly1305", "ChaCha20Poly1305")]
    public async Task SaveFileAsync_WithOlderKeyVersion_ReEncryptsWithNewKekBytes(string dekType, string kekType)
    {
        // Note: Both DEK and KEK services must stay the same - only KEK bytes (key version) can change
        // Setup key store with both versions
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create service with encryption combo
        var dekService = CreateEncryptionService(dekType, keyStore);
        var kekService = CreateEncryptionService(kekType, keyStore);
        var v1EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV1 = CreateService(encryptionService: v1EncryptionService);

        // Encrypt file with v1
        var originalData = Encoding.UTF8.GetBytes($"Data to re-encrypt with v1 ({dekType} DEK, {kekType} KEK)");
        var saveResultV1 = await serviceV1.SaveFileAsync(originalData, "reencrypt.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResultV1.DataEncryptionKeyId);
        Assert.Equal("1", saveResultV1.DataEncryptionKeyVersion);

        // Switch to v2 with different KEK bytes (but same KEK service type)
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2EncryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV2 = CreateService(encryptionService: v2EncryptionService);

        // Decrypt with v2 service (uses v1 key from key store, same services)
        var decryptedData = await serviceV2.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedData);

        // Delete old file
        await serviceV2.DeleteFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Re-encrypt with v2 (different KEK bytes, same services)
        var saveResultV2 = await serviceV2.SaveFileAsync(decryptedData, "reencrypt.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveResultV2.DataEncryptionKeyId);
        Assert.Equal("2", saveResultV2.DataEncryptionKeyVersion);

        // Verify new file decrypts correctly
        var decryptedV2 = await serviceV2.GetFileAsync(saveResultV2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV2);

        // Verify metadata shows v2
        var metadata = await serviceV2.GetMetadataAsync(saveResultV2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("2", metadata.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task SaveFileAsync_MultipleKeyVersionsWithDifferentKekBytes_AllWork()
    {
        // Note: Using same DEK and KEK services for all versions - only KEK bytes (key version) changes
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "3", "kek-v3", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // V1: AesGcm DEK + AesGcm KEK
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dekService = new AesGcmEncryptionService(keyStore);
        var kekService = new AesGcmEncryptionService(keyStore);
        var v1Encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV1 = CreateService(encryptionService: v1Encryption);
        var data1 = "File encrypted with v1 (AesGcm DEK + AesGcm KEK)"u8.ToArray();
        var save1 = await serviceV1.SaveFileAsync(data1, "v1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, save1.DataEncryptionKeyId);
        Assert.Equal("1", save1.DataEncryptionKeyVersion);

        // V2: Same services, different KEK bytes
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2Encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV2 = CreateService(encryptionService: v2Encryption);
        var data2 = "File encrypted with v2 (AesGcm DEK + AesGcm KEK, different KEK bytes)"u8.ToArray();
        var save2 = await serviceV2.SaveFileAsync(data2, "v2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, save2.DataEncryptionKeyId);
        Assert.Equal("2", save2.DataEncryptionKeyVersion);

        // V3: Same services, different KEK bytes
        await keyStore.SetCurrentVersionAsync(keyId, "3", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v3Encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV3 = CreateService(encryptionService: v3Encryption);
        var data3 = "File encrypted with v3 (AesGcm DEK + AesGcm KEK, different KEK bytes)"u8.ToArray();
        var save3 = await serviceV3.SaveFileAsync(data3, "v3.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, save3.DataEncryptionKeyId);
        Assert.Equal("3", save3.DataEncryptionKeyVersion);

        // Verify all files can be decrypted with their respective services
        var dec1 = await serviceV1.GetFileAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dec2 = await serviceV2.GetFileAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dec3 = await serviceV3.GetFileAsync(save3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(data1, dec1);
        Assert.Equal(data2, dec2);
        Assert.Equal(data3, dec3);

        // Verify v3 service can decrypt v1 and v2 files (using key store, same services)
        var dec1WithV3 = await serviceV3.GetFileAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dec2WithV3 = await serviceV3.GetFileAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(data1, dec1WithV3);
        Assert.Equal(data2, dec2WithV3);
    }

    [Fact]
    public async Task SaveFileAsync_CrossVersionDecryptionWithDifferentKekBytes_Works()
    {
        // Note: Using same DEK and KEK services - only KEK bytes (key version) changes
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyFromStringAsync(keyId, "2", "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // V1: AesGcm DEK + AesGcm KEK
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var dekService = new AesGcmEncryptionService(keyStore);
        var kekService = new AesGcmEncryptionService(keyStore);
        var v1Encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV1 = CreateService(encryptionService: v1Encryption);
        var originalData = "Cross-version decryption test"u8.ToArray();
        var saveV1 = await serviceV1.SaveFileAsync(originalData, "cross.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, saveV1.DataEncryptionKeyId);
        Assert.Equal("1", saveV1.DataEncryptionKeyVersion);

        // V2: Same services, different KEK bytes
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var v2Encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dekService, kekService, keyStore);
        using var serviceV2 = CreateService(encryptionService: v2Encryption);

        // V2 service should be able to decrypt V1 file (uses key store to get v1 key, same services)
        var decrypted = await serviceV2.GetFileAsync(saveV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decrypted);

        // V1 service should be able to decrypt V1 file
        var decryptedV1 = await serviceV1.GetFileAsync(saveV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decryptedV1);
    }

    [Fact]
    public async Task MigrateDeksAsync_BasicKeyRotation_MigratesSuccessfully()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with version 1
        var originalData = "Data to migrate"u8.ToArray();
        var saveResultV1 = await service.SaveFileAsync(originalData, "migrate.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", saveResultV1.DataEncryptionKeyVersion);

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, await keyStore.GetCurrentVersionAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));

        // Migrate DEKs
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(1, migrationResult.TotalFilesFound);
        Assert.Equal(1, migrationResult.SuccessfullyMigrated);
        Assert.Equal(0, migrationResult.Failed);

        // Verify metadata was updated
        var metadata = await service.GetMetadataAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, metadata.DataEncryptionKeyVersion);
        Assert.Equal(keyId, metadata.DataEncryptionKeyId);
        Assert.NotEqual(saveResultV1.EncryptedDataEncryptionKey, metadata.EncryptedDataEncryptionKey);

        // Verify file can still be decrypted
        var decrypted = await service.GetFileAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public async Task MigrateDeksAsync_MultipleFiles_MigratesAllSuccessfully()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt multiple files with version 1
        var data1 = "File 1"u8.ToArray();
        var data2 = "File 2"u8.ToArray();
        var data3 = "File 3"u8.ToArray();
        var save1 = await service.SaveFileAsync(data1, "file1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save2 = await service.SaveFileAsync(data2, "file2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save3 = await service.SaveFileAsync(data3, "file3.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.All([save1, save2, save3], r => Assert.Equal("1", r.DataEncryptionKeyVersion));

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate all files
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(3, migrationResult.TotalFilesFound);
        Assert.Equal(3, migrationResult.SuccessfullyMigrated);
        Assert.Equal(0, migrationResult.Failed);

        // Verify all files were migrated
        var metadata1 = await service.GetMetadataAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata2 = await service.GetMetadataAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata3 = await service.GetMetadataAsync(save3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.All([metadata1, metadata2, metadata3], m => Assert.Equal(version2, m.DataEncryptionKeyVersion));

        // Verify all files can still be decrypted
        Assert.Equal(data1, await service.GetFileAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal(data2, await service.GetFileAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal(data3, await service.GetFileAsync(save3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task MigrateDeksAsync_WithDifferentKeyId_MigratesSuccessfully()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "target-key";
        var keyStore = CreateKeyStoreWithKey(sourceKeyId, "1", "source-kek-v1");
        var targetVersion = await keyStore.UpdateKeyFromStringAsync(targetKeyId, "target-kek-v1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(targetKeyId, targetVersion, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with source key
        var originalData = "Data to migrate between keys"u8.ToArray();
        var saveResult = await service.SaveFileAsync(originalData, "migrate.txt", encrypt: true, keyId: sourceKeyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", saveResult.DataEncryptionKeyVersion);
        Assert.Equal(sourceKeyId, saveResult.DataEncryptionKeyId);

        // Migrate to target keyId
        var migrationResult = await service.MigrateDeksAsync(sourceKeyId, "1", targetKeyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(1, migrationResult.SuccessfullyMigrated);

        // Verify metadata was updated
        var metadata = await service.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(targetKeyId, metadata.DataEncryptionKeyId);
        Assert.Equal(targetVersion, metadata.DataEncryptionKeyVersion);

        // Verify file can be decrypted with target key
        var decrypted = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public async Task MigrateDeksAsync_AlreadyMigrated_IsIdempotent()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with version 1
        var originalData = "Data to migrate"u8.ToArray();
        var saveResultV1 = await service.SaveFileAsync(originalData, "migrate.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate first time
        var migrationResult1 = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult1.AllSucceeded);
        Assert.Equal(1, migrationResult1.SuccessfullyMigrated);

        // Verify file was migrated to version 2
        var metadataAfterFirst = await service.GetMetadataAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, metadataAfterFirst.DataEncryptionKeyVersion);

        // Migrate again - file is now at version 2, so searching for version 1 files finds 0
        // This demonstrates idempotency: running migration again for version 1 finds nothing (already migrated)
        var migrationResult2 = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult2.AllSucceeded);
        Assert.Equal(0, migrationResult2.TotalFilesFound); // No files found at version 1 (already migrated)
        Assert.Equal(0, migrationResult2.SuccessfullyMigrated);

        // But if we search for version 2 files and try to migrate to version 2, it should skip (already at target)
        var migrationResult3 = await service.MigrateDeksAsync(keyId, version2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult3.AllSucceeded);
        Assert.Equal(1, migrationResult3.TotalFilesFound);
        Assert.Equal(1, migrationResult3.SuccessfullyMigrated); // Should skip already migrated file (counts as success)

        // Verify metadata is still correct
        var metadata = await service.GetMetadataAsync(saveResultV1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, metadata.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task MigrateDeksAsync_NoFilesFound_ReturnsEmptyResult()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Try to migrate non-existent version
        var migrationResult = await service.MigrateDeksAsync(keyId, "999", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(0, migrationResult.TotalFilesFound);
        Assert.Equal(0, migrationResult.SuccessfullyMigrated);
        Assert.Equal(0, migrationResult.Failed);
        Assert.Empty(migrationResult.FailedFileIds);
        Assert.Empty(migrationResult.Errors);
    }

    [Fact]
    public async Task MigrateDeksAsync_AllVersions_MigratesAllVersions()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt files with version 1
        var save1 = await service.SaveFileAsync(Encoding.UTF8.GetBytes("File 1"), "file1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 2 and encrypt more files
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save2 = await service.SaveFileAsync("File 2"u8.ToArray(), "file2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 3
        var version3 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v3", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate all versions (sourceKeyVersion = null)
        var migrationResult = await service.MigrateDeksAsync(keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(2, migrationResult.TotalFilesFound); // Both files should be found
        Assert.Equal(2, migrationResult.SuccessfullyMigrated);

        // Verify both files were migrated to version 3
        var metadata1 = await service.GetMetadataAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata2 = await service.GetMetadataAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version3, metadata1.DataEncryptionKeyVersion);
        Assert.Equal(version3, metadata2.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task MigrateDeksAsync_BatchProcessing_ProcessesInBatches()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Create 5 files
        var files = new List<FileStoreResult>();
        for (var i = 0; i < 5; i++) {
            var data = Encoding.UTF8.GetBytes($"File {i}");
            var saveResult = await service.SaveFileAsync(data, $"file{i}.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            files.Add(saveResult);
        }

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate with batch size of 2
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", batchSize: 2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(5, migrationResult.TotalFilesFound);
        Assert.Equal(5, migrationResult.SuccessfullyMigrated);

        // Verify all files were migrated
        foreach (var file in files) {
            var metadata = await service.GetMetadataAsync(file.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.Equal(version2, metadata.DataEncryptionKeyVersion);
        }
    }

    [Fact]
    public async Task MigrateDeksAsync_NoEncryptionService_Throws()
    {
        using var service = CreateService(); // No encryption service
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateDeksAsync("test-key", "1", ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task MigrateDeksAsync_InvalidBatchSize_Throws()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => service.MigrateDeksAsync(keyId, "1", batchSize: 0, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => service.MigrateDeksAsync(keyId, "1", batchSize: -1, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task MigrateDeksAsync_NonExistentTargetKey_Throws()
    {
        const string sourceKeyId = "source-key";
        const string targetKeyId = "non-existent-key";
        var keyStore = CreateKeyStoreWithKey(sourceKeyId, "1", "source-kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with source key
        await service.SaveFileAsync("Test"u8.ToArray(), "test.txt", encrypt: true, keyId: sourceKeyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Try to migrate to non-existent target key
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateDeksAsync(sourceKeyId, "1", targetKeyId, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task MigrateDeksAsync_MixedVersions_MigratesOnlySpecifiedVersion()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with version 1
        var save1 = await service.SaveFileAsync("File 1"u8.ToArray(), "file1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 2 and encrypt more files
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save2 = await service.SaveFileAsync("File 2"u8.ToArray(), "file2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 3
        var version3 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v3", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate only version 1 files
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(1, migrationResult.TotalFilesFound); // Only version 1 file
        Assert.Equal(1, migrationResult.SuccessfullyMigrated);

        // Verify version 1 file was migrated to version 3
        var metadata1 = await service.GetMetadataAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version3, metadata1.DataEncryptionKeyVersion);

        // Verify version 2 file was not migrated
        var metadata2 = await service.GetMetadataAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, metadata2.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task MigrateDeksAsync_WithSpecificTargetVersion_MigratesToSpecificVersion()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v3", TestContext.Current.CancellationToken).ConfigureAwait(false);
        // Keep current version at 1 so file encrypts with version 1
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Encrypt file with version 1 (current version)
        var saveResult = await service.SaveFileAsync("Test"u8.ToArray(), "test.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", saveResult.DataEncryptionKeyVersion);

        // Migrate to specific version 2 (not current version 3)
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", targetKeyVersion: version2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);

        // Verify file was migrated to version 2 (not current version 3)
        var metadata = await service.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, metadata.DataEncryptionKeyVersion);

        // Verify file can still be decrypted
        var decrypted = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("Test"u8.ToArray(), decrypted);
    }

    [Fact]
    public async Task MigrateDeksAsync_NonEncryptedFiles_SkipsThem()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);

        // Save encrypted file
        var encryptedFile = await service.SaveFileAsync("Encrypted"u8.ToArray(), "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Save non-encrypted file
        var plainFile = await service.SaveFileAsync("Plain"u8.ToArray(), "plain.txt", encrypt: false, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rotate to version 2
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Migrate - should only find encrypted file
        var migrationResult = await service.MigrateDeksAsync(keyId, "1", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(migrationResult.AllSucceeded);
        Assert.Equal(1, migrationResult.TotalFilesFound); // Only encrypted file
        Assert.Equal(1, migrationResult.SuccessfullyMigrated);

        // Verify encrypted file was migrated
        var encryptedMetadata = await service.GetMetadataAsync(encryptedFile.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(version2, encryptedMetadata.DataEncryptionKeyVersion);

        // Verify plain file was not affected
        var plainMetadata = await service.GetMetadataAsync(plainFile.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(plainMetadata.IsEncrypted);
    }

    [Fact]
    public async Task RotateDeksAsync_WithSpecificFileIds_RotatesOnlyRequestedFiles()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var data1 = "Rotate file 1"u8.ToArray();
        var data2 = "Rotate file 2"u8.ToArray();
        var data3 = "Rotate file 3"u8.ToArray();
        var save1 = await service.SaveFileAsync(data1, "file1.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save2 = await service.SaveFileAsync(data2, "file2.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var save3 = await service.SaveFileAsync(data3, "file3.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var rotationResult = await service.RotateDeksAsync([save1.Id, save3.Id], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(rotationResult.AllSucceeded);
        Assert.Equal(2, rotationResult.TotalFilesFound);
        Assert.Equal(2, rotationResult.SuccessfullyMigrated);
        Assert.Equal(0, rotationResult.Failed);
        var metadata1 = await service.GetMetadataAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata2 = await service.GetMetadataAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata3 = await service.GetMetadataAsync(save3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("1", metadata1.DataEncryptionKeyVersion);
        Assert.Equal("1", metadata3.DataEncryptionKeyVersion);
        Assert.NotEqual(save1.EncryptedDataEncryptionKey, metadata1.EncryptedDataEncryptionKey);
        Assert.NotEqual(save3.EncryptedDataEncryptionKey, metadata3.EncryptedDataEncryptionKey);
        Assert.Equal(save2.EncryptedDataEncryptionKey, metadata2.EncryptedDataEncryptionKey);
        Assert.Equal(data1, await service.GetFileAsync(save1.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal(data2, await service.GetFileAsync(save2.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal(data3, await service.GetFileAsync(save3.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task RotateDeksAsync_WithTargetKeyId_UsesCurrentTargetVersion()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var originalData = "Rotate into current version"u8.ToArray();
        var saveResult = await service.SaveFileAsync(originalData, "rotate.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var version2 = await keyStore.UpdateKeyFromStringAsync(keyId, "kek-v2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var rotationResult = await service.RotateDeksAsync([saveResult.Id], keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(rotationResult.AllSucceeded);
        Assert.Equal(1, rotationResult.SuccessfullyMigrated);
        var metadata = await service.GetMetadataAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(keyId, metadata.DataEncryptionKeyId);
        Assert.Equal(version2, metadata.DataEncryptionKeyVersion);
        Assert.NotEqual(saveResult.EncryptedDataEncryptionKey, metadata.EncryptedDataEncryptionKey);
        Assert.Equal(originalData, await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task RotateDeksAsync_WithMissingOrPlainFiles_ReturnsFailures()
    {
        const string keyId = "test-key";
        var keyStore = CreateKeyStoreWithKey(keyId, "1", "kek-v1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var encryptedFile = await service.SaveFileAsync("Encrypted"u8.ToArray(), "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var plainFile = await service.SaveFileAsync("Plain"u8.ToArray(), "plain.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var missingFileId = Guid.NewGuid();
        var rotationResult = await service.RotateDeksAsync([encryptedFile.Id, plainFile.Id, missingFileId], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(rotationResult.AllSucceeded);
        Assert.Equal(3, rotationResult.TotalFilesFound);
        Assert.Equal(1, rotationResult.SuccessfullyMigrated);
        Assert.Equal(2, rotationResult.Failed);
        Assert.Contains(plainFile.Id, rotationResult.FailedFileIds);
        Assert.Contains(missingFileId, rotationResult.FailedFileIds);
        Assert.Equal(2, rotationResult.Errors.Count);
        var encryptedMetadata = await service.GetMetadataAsync(encryptedFile.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotEqual(encryptedFile.EncryptedDataEncryptionKey, encryptedMetadata.EncryptedDataEncryptionKey);
        var plainMetadata = await service.GetMetadataAsync(plainFile.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(plainMetadata.IsEncrypted);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_Basic_SavesFileSuccessfully()
    {
        using var service = CreateService();
        var testData = "Hello, World from file!"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test.txt", result.OriginalFileName);
        Assert.Equal(testData.Length, result.OriginalFileSize);
        Assert.False(result.IsCompressed);
        Assert.False(result.IsEncrypted);
        Assert.NotNull(result.OriginalFileHash);
        Assert.NotNull(result.SourceFileHash);
        Assert.True(File.Exists(Path.Combine(_tempSession.SessionDirectory, GetSubPath(result.Id, ""))));
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithoutOriginalFileName_UsesFileName()
    {
        using var service = CreateService();
        var testData = "Test content"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result.OriginalFileName);
        Assert.Equal(Path.GetFileName(tempFile), result.OriginalFileName);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithCompression_CompressesFile()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('A', 1000) + "Compress me!" + new string('B', 1000));
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "compressed.txt", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.NotNull(result.CompressionAlgorithm);
        Assert.NotNull(result.CompressedFileSize);
        Assert.True(result.CompressedFileSize < result.OriginalFileSize);
        Assert.NotNull(result.CompressedFileHash);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithEncryption_EncryptsFile()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = "Encrypt this secret message from file"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.EncryptedFileSize);
        Assert.NotNull(result.EncryptedFileHash);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.Equal(keyId, result.DataEncryptionKeyId);
        Assert.NotNull(result.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithCompressionAndEncryption_ProcessesBoth()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressionService = new CompressionService();
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(compressionService: compressionService, encryptionService: encryptionService);
        var tempFile = await _tempSession.CreateFileAsync(new string('Z', 1000) + "Compress and encrypt from file!" + new string('W', 1000), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "both.txt", true, true, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.CompressionAlgorithm);
        Assert.NotNull(result.CompressedFileSize);
        Assert.NotNull(result.EncryptedFileSize);
        Assert.NotNull(result.EncryptedDataEncryptionKey);
        Assert.Equal(keyId, result.DataEncryptionKeyId);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_GetFileAsync_RetrievesCorrectly()
    {
        using var service = CreateService();
        var testData = "Retrieve test from file"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult = await service.SaveFileAsync(tempFile, "retrieve.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithCompression_GetFileAsync_DecompressesCorrectly()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('X', 1000) + "Decompress from file!" + new string('Y', 1000));
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult = await service.SaveFileAsync(tempFile, "compressed.txt", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithEncryption_GetFileAsync_DecryptsCorrectly()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(encryptionService: encryptionService);
        var testData = "Decrypt from file"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult = await service.SaveFileAsync(tempFile, "encrypted.txt", encrypt: true, keyId: keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithCompressionAndEncryption_GetFileAsync_ProcessesCorrectly()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-kek-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressionService = new CompressionService();
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        var encryptionService = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
        using var service = CreateService(compressionService: compressionService, encryptionService: encryptionService);
        var testData = Encoding.UTF8.GetBytes(new string('M', 1000) + "Round trip from file!" + new string('N', 1000));
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var saveResult = await service.SaveFileAsync(tempFile, "both.txt", true, true, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_NonExistentFile_ThrowsFileNotFoundException()
    {
        using var service = CreateService();
        var nonExistentFile = _tempSession.GetFilePath();
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.SaveFileAsync(nonExistentFile, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_EmptyFile_ThrowsArgumentException()
    {
        using var service = CreateService();
        var emptyFile = _tempSession.TouchFile();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveFileAsync(emptyFile, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_NullPath_ThrowsArgumentNullException()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveFileAsync((string)null!, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithChunkSize_UsesProvidedChunkSize()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        var testData = Encoding.UTF8.GetBytes(new string('A', 5000));
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "chunksize.txt", true, chunkSize: 2048, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.NotNull(result.CompressedFileSize);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_LargeFile_HandlesSuccessfully()
    {
        using var service = CreateService();
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var tempFile = await _tempSession.CreateFileAsync(largeData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "large.bin", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(largeData.Length, result.OriginalFileSize);
        var retrievedData = await service.GetFileAsync(result.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(largeData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithCompression_LargeFile_CompressesWell()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService);
        // Create data that compresses well (repeating patterns)
        var largeData = new byte[1024 * 100]; // 100KB
        for (var i = 0; i < largeData.Length; i++)
            largeData[i] = (byte)(i % 256);

        var tempFile = await _tempSession.CreateFileAsync(largeData, "large-compressed.bin", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "large-compressed.bin", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsCompressed);
        Assert.NotNull(result.CompressedFileSize);
        Assert.True(result.CompressedFileSize < result.OriginalFileSize);
        var retrievedData = await service.GetFileAsync(result.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(largeData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithPathPrefix_StoresInPrefix()
    {
        using var service = CreateService();
        var testData = "Path prefix test"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, "prefix.txt", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await service.SaveFileAsync(tempFile, "prefix.txt", pathPrefix: "test/prefix", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal("test/prefix", result.PathPrefix);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_ProducesSameHashAsByteArray()
    {
        using var service = CreateService();
        var testData = "Hash comparison test"u8.ToArray();
        var tempFile = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var resultFromFile = await service.SaveFileAsync(tempFile, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var resultFromBytes = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(resultFromFile.OriginalFileHash, resultFromBytes.OriginalFileHash);
        Assert.Equal(resultFromFile.OriginalFileSize, resultFromBytes.OriginalFileSize);
    }

    [Fact]
    public async Task SaveFileAsync_FromFilePath_WithDuplicateDetection_DetectsDuplicate()
    {
        using var service = CreateService(true);
        var testData = "Duplicate detection test"u8.ToArray();
        var tempFile1 = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var tempFile2 = await _tempSession.CreateFileAsync(testData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result1 = await service.SaveFileAsync(tempFile1, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result2 = await service.SaveFileAsync(tempFile2, "test2.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(result1.OriginalFileHash, result2.OriginalFileHash);
    }

    [Fact]
    public async Task SaveFileAsync_WithSha256_StoresHashAlgorithmInMetadata()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha256);
        var testData = "Sha256 test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha256, result.HashAlgorithm);
        Assert.NotNull(result.OriginalFileHash);
        Assert.Equal(32, result.OriginalFileHash.Length); // SHA-256 produces 32 bytes
    }

    [Fact]
    public async Task SaveFileAsync_WithSha384_StoresHashAlgorithmInMetadata()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha384);
        var testData = "Sha384 test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha384, result.HashAlgorithm);
        Assert.NotNull(result.OriginalFileHash);
        Assert.Equal(48, result.OriginalFileHash.Length); // SHA-384 produces 48 bytes
    }

    [Fact]
    public async Task SaveFileAsync_WithSha512_StoresHashAlgorithmInMetadata()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha512);
        var testData = "Sha512 test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha512, result.HashAlgorithm);
        Assert.NotNull(result.OriginalFileHash);
        Assert.Equal(64, result.OriginalFileHash.Length); // SHA-512 produces 64 bytes
    }

    [Fact]
    public async Task SaveFileAsync_WithMd5_StoresHashAlgorithmInMetadata()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Md5);
        var testData = "Md5 test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Md5, result.HashAlgorithm);
        Assert.NotNull(result.OriginalFileHash);
        Assert.Equal(16, result.OriginalFileHash.Length); // MD5 produces 16 bytes
    }

    [Fact]
    public async Task SaveFileAsync_WithSha1_StoresHashAlgorithmInMetadata()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha1);
        var testData = "Sha1 test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha1, result.HashAlgorithm);
        Assert.NotNull(result.OriginalFileHash);
        Assert.Equal(20, result.OriginalFileHash.Length); // SHA-1 produces 20 bytes
    }

    [Fact]
    public async Task SaveFileAsync_DifferentAlgorithms_ProduceDifferentHashLengths()
    {
        var testData = "Same content"u8.ToArray();
        using var serviceSha256 = CreateService(hashAlgorithm: HashAlgorithm.Sha256);
        using var serviceSha384 = CreateService(hashAlgorithm: HashAlgorithm.Sha384);
        using var serviceMd5 = CreateService(hashAlgorithm: HashAlgorithm.Md5);
        var result256 = await serviceSha256.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result384 = await serviceSha384.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var resultMd5 = await serviceMd5.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(32, result256.OriginalFileHash!.Length);
        Assert.Equal(48, result384.OriginalFileHash!.Length);
        Assert.Equal(16, resultMd5.OriginalFileHash!.Length);
        Assert.NotEqual(result256.OriginalFileHash, result384.OriginalFileHash);
        Assert.NotEqual(result256.OriginalFileHash, resultMd5.OriginalFileHash);
    }

    [Fact]
    public async Task GetFileAsync_WithSha384_RoundTrip()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha384);
        var testData = "Round trip Sha384"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task GetFileAsync_WithSha512_RoundTrip()
    {
        using var service = CreateService(hashAlgorithm: HashAlgorithm.Sha512);
        var testData = "Round trip Sha512"u8.ToArray();
        var saveResult = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    [Fact]
    public async Task SaveFileAsync_WithSha384_AndDuplicateDetection_DetectsDuplicate()
    {
        using var service = CreateService(true, hashAlgorithm: HashAlgorithm.Sha384);
        var testData = "Duplicate with Sha384"u8.ToArray();
        var result1 = await service.SaveFileAsync(testData, "first.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result2 = await service.SaveFileAsync(testData, "second.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(HashAlgorithm.Sha384, result2.HashAlgorithm);
    }

    [Fact]
    public async Task SaveFileAsync_Default_UsesSha256()
    {
        using var service = CreateService();
        var testData = "Default algorithm test"u8.ToArray();
        var result = await service.SaveFileAsync(testData, "test.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha256, result.HashAlgorithm);
        Assert.Equal(32, result.OriginalFileHash!.Length);
    }

    [Fact]
    public async Task SaveFileAsync_WithCompressionAndSha384_StoresAndRetrieves()
    {
        var compressionService = new CompressionService();
        using var service = CreateService(compressionService: compressionService, hashAlgorithm: HashAlgorithm.Sha384);
        var testData = Encoding.UTF8.GetBytes(new string('A', 500) + "Compress with Sha384" + new string('B', 500));
        var saveResult = await service.SaveFileAsync(testData, "compressed.txt", true, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HashAlgorithm.Sha384, saveResult.HashAlgorithm);
        Assert.True(saveResult.IsCompressed);
        var retrievedData = await service.GetFileAsync(saveResult.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(testData, retrievedData);
    }

    private string GetSubPath(Guid fileId, string extension)
    {
        var idString = fileId.ToString("N");
        var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
        var fileName = fileId.ToString("N") + extension;
        return Path.Combine(subDir, fileName);
    }
}