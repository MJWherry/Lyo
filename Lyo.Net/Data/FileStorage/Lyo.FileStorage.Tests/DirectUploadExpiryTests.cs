using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Tests.Support;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

public sealed class DirectUploadExpiryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-fs-expiry-" + LyoGuid.CreateV7().ToString("N"));

    private readonly CaptureAuditHandler _audit = new();

    private readonly LocalFileMetadataStore _store;

    private readonly VerifyingFakeFileStorageService _service;

    public DirectUploadExpiryTests()
    {
        Directory.CreateDirectory(_root);
        _store = new(_root);
        _service = new(new DiskFileStorageOptions { RootDirectoryPath = _root, PendingDirectUploadTtl = TimeSpan.FromHours(1) }, _store, [_audit]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private async Task<Guid> BeginBackdatedAsync(TimeSpan age)
    {
        var begin = await _service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 100, OriginalFileName = "a.bin" }, TestCt);
        var meta = await _store.GetMetadataAsync(begin.FileId, TestCt);
        await _store.SaveMetadataAsync(begin.FileId, meta with { Timestamp = DateTime.UtcNow - age }, TestCt);
        return begin.FileId;
    }

    [Fact]
    public async Task SweepExpiredDirectUploadsAsync_PendingPastTtl_DeletesObjectTombstonesRowAndAudits()
    {
        var fileId = await BeginBackdatedAsync(TimeSpan.FromHours(2));
        _service.PutObject(fileId, TestData.Create(100));

        var swept = await _service.SweepExpiredDirectUploadsAsync(ct: TestCt);

        Assert.Equal(1, swept);
        Assert.False(_service.HasObject(fileId));
        await Assert.ThrowsAsync<FileNotFoundException>(() => _store.GetMetadataAsync(fileId, TestCt));
        Assert.Contains(_audit.Events, e => e.EventType == FileAuditEventType.DirectUploadFailed && e.FileId == fileId && e.Outcome == FileAuditOutcome.Failure);
    }

    [Fact]
    public async Task SweepExpiredDirectUploadsAsync_ObjectNeverUploaded_StillTombstonesRow()
    {
        var fileId = await BeginBackdatedAsync(TimeSpan.FromHours(2));
        Assert.Equal(1, await _service.SweepExpiredDirectUploadsAsync(ct: TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => _store.GetMetadataAsync(fileId, TestCt));
    }

    [Fact]
    public async Task SweepExpiredDirectUploadsAsync_PendingWithinTtl_LeavesItPending()
    {
        var fileId = await BeginBackdatedAsync(TimeSpan.FromMinutes(10));
        _service.PutObject(fileId, TestData.Create(100));
        Assert.Equal(0, await _service.SweepExpiredDirectUploadsAsync(ct: TestCt));
        Assert.True(_service.HasObject(fileId));
        Assert.Equal(FileAvailability.PendingDirectUpload, (await _store.GetMetadataAsync(fileId, TestCt)).Availability);
    }

    [Fact]
    public async Task SweepExpiredDirectUploadsAsync_RespectsMaxUploads()
    {
        for (var i = 0; i < 3; i++)
            await BeginBackdatedAsync(TimeSpan.FromHours(2));

        Assert.Equal(2, await _service.SweepExpiredDirectUploadsAsync(2, TestCt));
        Assert.Equal(1, await _service.SweepExpiredDirectUploadsAsync(2, TestCt));
    }

    [Fact]
    public async Task BeginDirectUploadAsync_UrlExpirationBeyondPendingTtl_Throws()
        => await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = 100, OriginalFileName = "a.bin", UrlExpiration = TimeSpan.FromHours(2) }, TestCt));
}
