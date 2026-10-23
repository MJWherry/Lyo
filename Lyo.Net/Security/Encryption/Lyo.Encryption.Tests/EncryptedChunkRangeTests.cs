using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.KeyStore;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

public sealed class EncryptedChunkRangeTests
{
    private CancellationToken Ct => TestCt;

    [Fact]
    public async Task DecryptFromChunkAsync_SkipInsideLaterChunk_ReturnsThatPlaintext()
    {
        const string keyId = "enc-key";
        const int chunkSize = 32;
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "test-kek-key-material-32bytes!!", Ct);
        await keyStore.SetCurrentVersionAsync(keyId, "1", Ct);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keyStore), keyStore);
        var plain = TestData.Create(80, TestData.Seed + 7);
        using var cipher = new MemoryStream();
        await svc.EncryptToStreamAsync(new MemoryStream(plain), cipher, keyId, chunkSize: chunkSize, ct: TestCt);
        cipher.Position = 0;
        using var layout = await svc.ReadEncryptedStreamLayoutAsync(cipher, TestCt);
        const int chunkIndex = 2;
        const int skip = 4;
        const int take = 10;
        cipher.Position = layout.HeaderLength + chunkIndex * layout.FrameSize;
        using var output = new MemoryStream();
        await svc.DecryptFromChunkAsync(layout.Header, cipher, output, chunkIndex, skip, take, layout.Dek, TestCt);
        var start = chunkIndex * layout.ChunkSize + skip;
        Assert.Equal(plain[start..(start + take)], output.ToArray());
    }
}