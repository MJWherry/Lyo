using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

public sealed class EncryptedByteRangeTests
{
    [Fact]
    public async Task OpenPlainByteRangeAsync_EncryptedFile_ReadsAtCiphertextChunk()
    {
        const string keyId = "enc-key";
        const int chunkSize = 32;
        using var encryption = CreateEncryption(keyId);
        using var scope = LocalFileStorageTestScope.Create(twoKeyEncryptionService: encryption);
        var payload = TestData.Create(80, TestData.Seed + 8);
        var saved = await scope.Storage.SaveFileAsync(payload, "clip.mp4", encrypt: true, keyId: keyId, chunkSize: chunkSize, ct: TestCt);
        var relative = CloudObjectKeyBuilder.FromMetadata(saved.Id, saved.SourceFileName, saved.PathPrefix);
        var cipherPath = Path.Combine(scope.Options.RootDirectoryPath, relative.Replace('/', Path.DirectorySeparatorChar));
        await using var cipher = File.OpenRead(cipherPath);
        using var layout = await encryption.ReadEncryptedStreamLayoutAsync(cipher, TestCt);
        const int chunkIndex = 2;
        const long start = chunkIndex * 32 + 4;
        const long end = start + 7;
        await using var slice = await scope.Storage.OpenPlainByteRangeAsync(saved, start, end, TestCt);
        using var buffered = new MemoryStream();
        await slice!.CopyToAsync(buffered, TestCt);
        Assert.Equal(payload[(int)start..((int)end + 1)], buffered.ToArray());
        Assert.Equal(layout.HeaderLength + chunkIndex * layout.FrameSize, scope.Storage.PlainByteRangeReadStart);
    }

    private static TwoKeyEncryptionService<IEncryptionService, IEncryptionService> CreateEncryption(string keyId)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyId, "1");
        return new(new AesGcmEncryptionService(keyStore), keyStore);
    }
}