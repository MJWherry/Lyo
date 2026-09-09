using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public class DirectUploadTests
{
    [Fact]
    public async Task ReceiveDirectPutAsync_ExceedsMaxUploadSize_Throws()
    {
        // The Local PUT receiver enforces FileStorageServiceBaseOptions.MaxUploadSizeBytes during the copy so an attacker cannot fill the disk before finalize re-checks the size.
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
            o.MaxUploadSizeBytes = 8;
            return o;
        });

        var begin = await scope.Storage.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 8, OriginalFileName = "tiny.bin" }, TestContext.Current.CancellationToken);

        var oversized = new byte[32];
        await using var ms = new MemoryStream(oversized);
        await Assert.ThrowsAnyAsync<FilePolicyRejectedException>(async ()
            => await scope.Storage.ReceiveDirectPutAsync(begin.FileId, ms, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_WithoutBody_EmitsFailureAudit()
    {
        var sink = new CaptureAuditHandler();
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.DirectUploadReceiveBaseUri = "https://tests.invalid";
                o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
                return o;
            }, new[] { sink });

        var begin = await scope.Storage.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 1024, OriginalFileName = "missing.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<FileNotFoundException>(() => scope.Storage.CompleteDirectUploadAsync(begin.FileId, ct: TestContext.Current.CancellationToken));
        Assert.Contains(
            sink.Events,
            e => (e.EventType == FileAuditEventType.DirectUploadFailed || e.EventType == FileAuditEventType.DirectUploadComplete) && e.Outcome == FileAuditOutcome.Failure);
    }
}
