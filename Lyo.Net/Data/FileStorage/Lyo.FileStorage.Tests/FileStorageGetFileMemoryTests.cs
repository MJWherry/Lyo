using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Streams;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// <c>GetFileAsync</c> returns one array, so it should cost one payload-sized allocation. It also has to keep raising exactly one retrieve event and one read audit, which is
/// what a rewrite onto the shared streaming decode pipeline is most likely to double up.
/// </summary>
[Collection(SerialAllocationCollection.Name)]
public sealed class FileStorageGetFileMemoryTests
{
    private const int PayloadBytes = 8 * 1024 * 1024;

    private static CompressionService CreateCompressionService()
        => new([new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory()]);

    private static ITwoKeyEncryptionService CreateEncryptionService(string keyId)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyId, "1");
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keyStore), keyStore);
    }

    [Fact]
    public async Task GetFileAsync_PlainFile_AllocatesRoughlyOnePayload()
    {
        using var scope = LocalFileStorageTestScope.Create();
        using var source = new SpanReadStream(new DeterministicPayloadStream(PayloadBytes));
        var saved = await scope.Storage.SaveFromStreamAsync(source, PayloadBytes, "large.bin", ct: TestCt);

        // Warm the read path so first-call JIT and static initialisation stay out of the measurement.
        Assert.Equal(PayloadBytes, (await scope.Storage.GetFileAsync(saved.Id, ct: TestCt)).Length);
        var before = GC.GetTotalAllocatedBytes(true);
        var data = await scope.Storage.GetFileAsync(saved.Id, ct: TestCt);
        var allocated = GC.GetTotalAllocatedBytes(true) - before;
        Assert.Equal(PayloadBytes, data.Length);
        Assert.True(
            allocated < 12L * 1024 * 1024, $"Reading {PayloadBytes:N0} bytes allocated {allocated:N0} bytes; the returned array should be the only payload-sized allocation.");
    }

    [Fact]
    public async Task GetFileAsync_EncryptedCompressedFile_RaisesOneRetrievedEventAndOneReadAudit()
    {
        var audit = new CaptureAuditHandler();
        using var scope = LocalFileStorageTestScope.Create(
            auditHandlers: [audit], compressionService: CreateCompressionService(), twoKeyEncryptionService: CreateEncryptionService("enc-key"));

        var payload = TestData.Create(256 * 1024);
        var saved = await scope.Storage.SaveFileAsync(payload, "both.bin", true, true, "enc-key", ct: TestCt);
        var retrieved = new List<FileRetrievedResult>();
        scope.Storage.FileRetrieved += (_, e) => retrieved.Add(e);
        audit.Events.Clear();
        var data = await scope.Storage.GetFileAsync(saved.Id, ct: TestCt);
        Assert.Equal(payload, data);
        var raised = Assert.Single(retrieved);
        Assert.Equal(saved.Id, raised.FileId);
        Assert.Equal(payload.LongLength, raised.FileSize);
        Assert.True(raised.WasCompressed);
        Assert.True(raised.WasEncrypted);
        var read = Assert.Single(audit.Events, e => e.EventType == FileAuditEventType.Read);
        Assert.Equal(FileAuditOutcome.Success, read.Outcome);
    }
}