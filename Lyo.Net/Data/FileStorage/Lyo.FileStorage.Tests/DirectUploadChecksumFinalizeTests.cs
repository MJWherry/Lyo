using System.Security.Cryptography;
using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Tests.Support;
using Lyo.Testing;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage.Tests;

public sealed class DirectUploadChecksumFinalizeTests : IDisposable
{
    private readonly CaptureAuditHandler _audit = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-fs-direct-" + LyoGuid.CreateV7().ToString("N"));

    public DirectUploadChecksumFinalizeTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private VerifyingFakeFileStorageService CreateService(Action<DiskFileStorageOptions>? configure = null)
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = _root, HashAlgorithm = HashAlgorithm.Sha256 };
        configure?.Invoke(options);
        return new(options, new LocalFileMetadataStore(_root), [_audit]);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_BackendReportsSizeAndChecksum_NeverReadsTheObject()
    {
        var service = CreateService();
        var payload = TestData.Create(4096, 11);
        var sha = SHA256.HashData(payload);
        service.VerifiedChecksumAlgorithm = HashAlgorithm.Sha256;
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedSha256 = sha }, TestCt);
        service.PutObject(begin.FileId, payload);
        service.ReportedAttributes = new(payload.Length, sha, HashAlgorithm.Sha256);
        service.ThrowOnBodyRead = true;
        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(payload.Length, done.OriginalFileSize);
        Assert.Equal(sha, done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Sha256, done.HashAlgorithm);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_BackendReportsMd5_RecordsMd5AsTheRowHashAlgorithm()
    {
        var service = CreateService();
        var payload = TestData.Create(2048, 12);
        var md5 = MD5.HashData(payload);
        service.VerifiedChecksumAlgorithm = HashAlgorithm.Md5;
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedContentMd5 = md5 }, TestCt);
        service.PutObject(begin.FileId, payload);
        service.ReportedAttributes = new(payload.Length, md5, HashAlgorithm.Md5);
        service.ThrowOnBodyRead = true;
        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        Assert.Equal(md5, done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Md5, done.HashAlgorithm);
        Assert.Equal(HashAlgorithm.Md5, (await service.GetMetadataAsync(begin.FileId, TestCt)).HashAlgorithm);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_ReportedChecksumMismatch_FailsFinalizeAndDoesNotMarkAvailable()
    {
        var service = CreateService();
        var payload = TestData.Create(4096, 13);
        service.VerifiedChecksumAlgorithm = HashAlgorithm.Sha256;
        var begin = await service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedSha256 = SHA256.HashData(payload) }, TestCt);

        service.PutObject(begin.FileId, payload);
        service.ReportedAttributes = new(payload.Length, SHA256.HashData(TestData.Create(4096, 14)), HashAlgorithm.Sha256);
        service.ThrowOnBodyRead = true;
        await Assert.ThrowsAsync<InvalidDataException>(() => service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt));
        Assert.Equal(FileAvailability.PendingDirectUpload, (await service.GetMetadataAsync(begin.FileId, TestCt)).Availability);
        Assert.Contains(_audit.Events, e => e.EventType == FileAuditEventType.DirectUploadFailed && e.FileId == begin.FileId && e.Outcome == FileAuditOutcome.Failure);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_BackendCannotReportChecksum_FallsBackToSpoolAndHash()
    {
        var service = CreateService();
        var payload = TestData.Create(4096, 15);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin" }, TestCt);
        service.PutObject(begin.FileId, payload);
        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        Assert.Equal(1, service.BodyReadCount);
        Assert.Equal(SHA256.HashData(payload), done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Sha256, done.HashAlgorithm);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_HeaderInspectionEnabled_ReadsOnlyThePrefix()
    {
        var service = CreateService(o => o.VerifyFileSignature = true);
        var payload = "%PDF-1.4\n"u8.ToArray().Concat(TestData.Create(64 * 1024, 16)).ToArray();
        var sha = SHA256.HashData(payload);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "report.pdf" }, TestCt);
        service.PutObject(begin.FileId, payload);
        service.ReportedAttributes = new(payload.Length, sha, HashAlgorithm.Sha256);
        service.ThrowOnBodyRead = true;
        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        Assert.Equal(FileAvailability.Available, done.Availability);
        var requested = Assert.Single(service.PrefixRequests);
        Assert.InRange(requested, 1, 1024);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_HeaderInspectionRejectsPrefix_DeletesTheObject()
    {
        var service = CreateService(o => o.VerifyFileSignature = true);
        var zip = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = zip.Length, OriginalFileName = "report.pdf" }, TestCt);
        service.PutObject(begin.FileId, zip);
        service.ReportedAttributes = new(zip.Length, SHA256.HashData(zip), HashAlgorithm.Sha256);
        service.ThrowOnBodyRead = true;
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt));
        Assert.False(service.HasObject(begin.FileId));
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_ReportedLengthDiffersFromExpected_Throws()
    {
        var service = CreateService();
        var payload = TestData.Create(100, 17);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 1000, OriginalFileName = "a.bin" }, TestCt);
        service.PutObject(begin.FileId, payload);
        service.ReportedAttributes = new(payload.Length, SHA256.HashData(payload), HashAlgorithm.Sha256);
        service.ThrowOnBodyRead = true;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteDirectUploadAsync(begin.FileId, new() { ExpectedByteLength = 99 }, TestCt));
        Assert.Contains("Expected byte length", ex.Message, StringComparison.Ordinal);
        Assert.Equal(FileAvailability.PendingDirectUpload, (await service.GetMetadataAsync(begin.FileId, TestCt)).Availability);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_DeclaredSha256DoesNotMatchUploadedBytes_FailsOnFallback()
    {
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.DirectUploadReceiveBaseUri = "https://tests.invalid";
                o.HashAlgorithm = HashAlgorithm.Sha256;
                return o;
            }, [_audit]);

        var payload = TestData.Create(512, 18);
        var begin = await scope.Storage.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedSha256 = SHA256.HashData(TestData.Create(512, 19)) }, TestCt);

        using (var ms = new MemoryStream(payload))
            await scope.Storage.ReceiveDirectPutAsync(begin.FileId, ms, TestCt);

        await Assert.ThrowsAsync<InvalidDataException>(() => scope.Storage.CompleteDirectUploadAsync(begin.FileId, ct: TestCt));
        Assert.Equal(FileAvailability.PendingDirectUpload, (await scope.Storage.GetMetadataAsync(begin.FileId, TestCt)).Availability);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    public async Task BeginDirectUploadAsync_ExpectedSha256WrongLength_Throws(int length)
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "a.bin", ExpectedSha256 = new byte[length] }, TestCt));
    }

    [Fact]
    public async Task BeginDirectUploadAsync_ExpectedContentMd5WrongLength_Throws()
    {
        var service = CreateService();
        service.VerifiedChecksumAlgorithm = HashAlgorithm.Md5;
        await Assert.ThrowsAsync<ArgumentException>(() => service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "a.bin", ExpectedContentMd5 = new byte[15] }, TestCt));
    }
}