using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public class MultipartAndAuditTests
{
    [Fact]
    public async Task LocalMultipart_BeginUploadComplete_RoundtripsPayload()
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
        var got = await scope.Storage.GetFileAsync(meta.Id, TestContext.Current.CancellationToken);
        Assert.Equal(payload, got);
    }

    [Fact]
    public async Task SaveFileAsync_WithAuditHandler_AppendsSaveEvent()
    {
        var sink = new CaptureAuditHandler();
        using var scope = LocalFileStorageTestScope.Create(o => o, new[] { sink });
        var data = "audit-me"u8.ToArray();
        await scope.Storage.SaveFileAsync(data, "a.txt", ct: TestContext.Current.CancellationToken);
        Assert.Contains(sink.Events, e => e.EventType == FileAuditEventType.Save && e.Outcome == FileAuditOutcome.Success);
    }

    [Fact]
    public async Task LocalMultipart_AbortAsync_MarksSessionAborted()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await multipart.AbortAsync(begin.SessionId, TestContext.Current.CancellationToken);
        // After abort the session is removed; further uploads should fail.
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.UploadPartAsync(
            begin.SessionId, 1, new MemoryStream(new byte[] { 1, 2, 3 }), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LocalMultipart_UploadPart_RejectsZeroPartNumber()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.UploadPartAsync(
            begin.SessionId, 0, new MemoryStream(new byte[] { 1 }), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LocalMultipart_Complete_WithMissingPart_Throws()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken);
        await multipart.UploadPartAsync(begin.SessionId, 1, new MemoryStream("part-1"u8.ToArray()), TestContext.Current.CancellationToken);
        // Complete claims part 2 exists even though only part 1 was uploaded.
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.CompleteAsync(
            new() {
                SessionId = begin.SessionId, Parts = new List<CompletedPart> { new() { PartNumber = 1, ETagOrBlockId = "p1" }, new() { PartNumber = 2, ETagOrBlockId = "p2" } }
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LocalMultipart_Encrypt_WithoutKeyId_Rejected()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        await Assert.ThrowsAnyAsync<Exception>(async () => await multipart.BeginAsync(
            new() { PartSizeBytes = 16 * 1024, Encrypt = true, KeyId = null }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DirectUpload_ReceiveExceedsMaxUploadSize_Throws()
    {
        // The Local PUT receiver enforces FileStorageServiceBaseOptions.MaxUploadSizeBytes during the copy so an attacker cannot exhaust disk before finalize re-checks the size.
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "Workbench/FileStorage/direct-upload";
            o.MaxUploadSizeBytes = 8;
            return o;
        });

        var begin = await scope.Storage.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 8, OriginalFileName = "tiny.bin" }, TestContext.Current.CancellationToken);

        // 32 bytes > 8 byte upload cap
        var oversized = new byte[32];
        await using var ms = new MemoryStream(oversized);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async ()
            => await scope.Storage.ReceiveWorkbenchDirectPutAsync(begin.FileId, ms, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DirectUpload_CompleteWithoutBody_EmitsFailureAudit()
    {
        var sink = new CaptureAuditHandler();
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.DirectUploadReceiveBaseUri = "https://tests.invalid";
                o.DirectUploadPutRouteRelativePath = "Workbench/FileStorage/direct-upload";
                return o;
            }, new[] { sink });

        var begin = await scope.Storage.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 1024, OriginalFileName = "missing.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<FileNotFoundException>(() => scope.Storage.CompleteDirectUploadAsync(begin.FileId, ct: TestContext.Current.CancellationToken));
        Assert.Contains(
            sink.Events,
            e => (e.EventType == FileAuditEventType.DirectUploadFailed || e.EventType == FileAuditEventType.DirectUploadComplete) && e.Outcome == FileAuditOutcome.Failure);
    }
}