using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Covers the atomic-replace behaviour of the local output stream: a write that faults partway must leave whatever was already at the target path intact, which stops a
/// failed DEK rotation from truncating the only copy of the ciphertext.
/// </summary>
public sealed class LocalAtomicOutputStreamTests
{
    private const string KeyId = "atomic-key";

    private static ITwoKeyEncryptionService CreateEncryptionService()
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(KeyId, "1", "atomic-kek");
        keyStore.SetCurrentVersion(KeyId, "1");
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keyStore), keyStore);
    }

    [Fact]
    public async Task RotateDeksAsync_EncryptFailsPartway_LeavesOriginalCiphertextReadable()
    {
        var ct = TestContext.Current.CancellationToken;
        var encryption = CreateEncryptionService();
        using var scope = LocalFileStorageTestScope.Create(twoKeyEncryptionService: encryption);
        var payload = TestData.Create(64 * 1024);
        var saved = await scope.Storage.SaveFileAsync(payload, "rotate.bin", encrypt: true, keyId: KeyId, ct: ct);

        using var failingStorage = new LocalFileStorageService(
            scope.Options, twoKeyEncryptionService: new FailingEncryptToStreamTwoKeyEncryptionService(encryption));

        var result = await failingStorage.RotateDeksAsync([saved.Id], ct: ct);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Contains(FailingEncryptToStreamTwoKeyEncryptionService.FailureMessage, StringComparison.Ordinal));

        // The half-written re-encryption never flushed, so it was discarded instead of replacing the file.
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: ct));
        var metadata = await scope.Storage.GetMetadataAsync(saved.Id, ct);
        Assert.Equal(saved.DataEncryptionKeyVersion, metadata.DataEncryptionKeyVersion);
        Assert.Equal(saved.EncryptedFileSize, metadata.EncryptedFileSize);
        Assert.Equal(saved.EncryptedFileHash, metadata.EncryptedFileHash);
    }

    [Fact]
    public async Task CreateOutputStreamAsync_FailedWrite_LeavesNoPartialFilesBehind()
    {
        var ct = TestContext.Current.CancellationToken;
        var encryption = CreateEncryptionService();
        using var scope = LocalFileStorageTestScope.Create(twoKeyEncryptionService: encryption);
        var payload = TestData.Create(64 * 1024);
        var saved = await scope.Storage.SaveFileAsync(payload, "rotate.bin", encrypt: true, keyId: KeyId, ct: ct);

        using var failingStorage = new LocalFileStorageService(
            scope.Options, twoKeyEncryptionService: new FailingEncryptToStreamTwoKeyEncryptionService(encryption));

        await failingStorage.RotateDeksAsync([saved.Id], ct: ct);
        Assert.Empty(Directory.GetFiles(scope.Options.RootDirectoryPath, "*.tmp", SearchOption.AllDirectories));
    }
}
