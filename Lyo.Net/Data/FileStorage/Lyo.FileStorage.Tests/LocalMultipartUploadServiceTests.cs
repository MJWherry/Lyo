using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public class LocalMultipartUploadServiceTests
{
    [Fact]
    public async Task BeginUploadComplete_RoundtripsPayload()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        var payload = "hello multipart"u8.ToArray();
        await multipart.UploadPartAsync(begin.SessionId, 1, new MemoryStream(payload), TestContext.Current.CancellationToken);
        var meta = await multipart.CompleteAsync(
            new() { SessionId = begin.SessionId, Parts = new List<CompletedPart> { new() { PartNumber = 1, ETagOrBlockId = "n/a" } } }, TestContext.Current.CancellationToken);

        Assert.Equal(begin.TargetFileId, meta.Id);
        var got = await scope.Storage.GetFileAsync(meta.Id, ct: TestContext.Current.CancellationToken);
        Assert.Equal(payload, got);
    }

    [Fact]
    public async Task AbortAsync_MarksSessionAborted()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await multipart.AbortAsync(begin.SessionId, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.UploadPartAsync(
            begin.SessionId, 1, new MemoryStream(new byte[] { 1, 2, 3 }), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UploadPart_ZeroPartNumber_Throws()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.UploadPartAsync(
            begin.SessionId, 0, new MemoryStream(new byte[] { 1 }), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Complete_MissingPart_Throws()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await multipart.UploadPartAsync(begin.SessionId, 1, new MemoryStream("part-1"u8.ToArray()), TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.CompleteAsync(
            new() {
                SessionId = begin.SessionId, Parts = new List<CompletedPart> { new() { PartNumber = 1, ETagOrBlockId = "p1" }, new() { PartNumber = 2, ETagOrBlockId = "p2" } }
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BeginAsync_EncryptWithoutKeyId_Throws()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.BeginAsync(
            new() { PartSizeBytes = 16 * 1024, Encrypt = true, KeyId = null }, TestContext.Current.CancellationToken));
    }
}
