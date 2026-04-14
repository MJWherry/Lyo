using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Tests;

public class MultipartAndAuditTests
{
    [Fact]
    public async Task LocalMultipart_BeginUploadComplete_RoundtripsPayload()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-mpu-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var options = new LocalFileStorageServiceOptions { RootDirectoryPath = root };
            var sessions = new InMemoryMultipartUploadSessionStore();
            var storage = new LocalFileStorageService(options, NullLoggerFactory.Instance, metadataService: null);
            var multipart = new LocalMultipartUploadService(storage, sessions, options);
            var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024 }, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var payload = "hello multipart"u8.ToArray();
            await multipart.UploadPartAsync(begin.SessionId, 1, new MemoryStream(payload), TestContext.Current.CancellationToken).ConfigureAwait(false);
            var meta = await multipart.CompleteAsync(
                new() { SessionId = begin.SessionId, Parts = new List<CompletedPart> { new() { PartNumber = 1, ETagOrBlockId = "n/a" } } }, TestContext.Current.CancellationToken).ConfigureAwait(false);

            Assert.Equal(begin.TargetFileId, meta.Id);
            var got = await storage.GetFileAsync(meta.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.Equal(payload, got);
        }
        finally {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SaveFileAsync_WithAuditHandler_AppendsSaveEvent()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var sink = new CaptureAuditHandler();
            var options = new LocalFileStorageServiceOptions { RootDirectoryPath = root };
            var storage = new LocalFileStorageService(options, NullLoggerFactory.Instance, metadataService: null, auditHandlers: new[] { sink });
            var data = "audit-me"u8.ToArray();
            await storage.SaveFileAsync(data, "a.txt", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.Contains(sink.Events, e => e.EventType == FileAuditEventType.Save && e.Outcome == FileAuditOutcome.Success);
        }
        finally {
            TryDeleteDirectory(root);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch {
            // test cleanup
        }
    }

    private sealed class CaptureAuditHandler : IFileAuditEventHandler
    {
        public List<FileAuditEvent> Events { get; } = [];

        public Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}