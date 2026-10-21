using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Local disk-only features: <see cref="IFileStorageDiagnosticsService" />, <see cref="LocalFileStorageService.CopyFileAsync" />,
/// <see cref="LocalFileStorageService.MoveFileAsync" />, rename, and direct-upload begin/receive/complete.
/// </summary>
/// <summary>Local disk-only features: <see cref="IFileStorageDiagnosticsService" />, copy, move, rename, direct-upload begin/receive/complete.</summary>
public sealed class LocalFileStorageDiagnosticsCopyDirectUploadTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IIOTempSession _tempSession;

    public LocalFileStorageDiagnosticsCopyDirectUploadTests(ITestOutputHelper output)
    {
        _loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // IOTempSession requires RootDirectory to already exist (it does not create the parent chain).
        var ioTempRoot = Path.Combine(Path.GetTempPath(), "lyo-filestorage-tests-io-temp-root-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(ioTempRoot);
        _tempSession = new IOTempSession(new() { RootDirectory = ioTempRoot }, _loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _tempSession.Dispose();
    }

    private LocalFileStorageService CreateService(Action<DiskFileStorageOptions>? configure = null)
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = _tempSession.SessionDirectory, ThrowOnDeleteNotFound = true, ThrowOnFileNotFound = true };
        configure?.Invoke(options);
        return new(options, _loggerFactory);
    }

    private static IFileStorageDiagnosticsService AsDiagnostics(LocalFileStorageService s) => s;

    [Fact]
    public async Task CopyFileAsync_CopiesBackingBytes_NewIdPreservesHashes()
    {
        using var service = CreateService();
        var plain = "copy-me-bytes"u8.ToArray();
        var saved = await service.SaveFileAsync(plain, "doc.txt", ct: TestCt);
        var copy = await service.CopyFileAsync(saved.Id, ct: TestCt);
        Assert.NotEqual(saved.Id, copy.Id);
        Assert.Equal(saved.OriginalFileHash, copy.OriginalFileHash);
        Assert.Equal(plain, await service.GetFileAsync(copy.Id, ct: TestCt));
        var copyMeta = await service.GetMetadataAsync(copy.Id, TestCt);
        Assert.Equal(saved.SourceFileHash, copyMeta.SourceFileHash);
    }

    [Fact]
    public async Task CopyFileAsync_WithPathPrefixOverride_StoresUnderNewPrefix()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", pathPrefix: "incoming", ct: TestCt);
        var copy = await service.CopyFileAsync(saved.Id, new() { PathPrefix = "archive" }, TestCt);
        var copyMeta = await service.GetMetadataAsync(copy.Id, TestCt);
        Assert.Equal("archive", copyMeta.PathPrefix);
        Assert.Equal("x"u8.ToArray(), await service.GetFileAsync(copy.Id, ct: TestCt));
    }

    [Fact]
    public async Task CopyFileAsync_WithTraversalPathPrefix_Throws()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", ct: TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CopyFileAsync(saved.Id, new() { PathPrefix = "archive/../../etc" }, TestCt));
    }

    [Fact]
    public async Task GetPreSignedReadUrlAsync_WithTraversalPathPrefix_Throws()
    {
        using var service = CreateService(o => o.AllowFileUriPresignedUrls = true);
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", ct: TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPreSignedReadUrlAsync(saved.Id, TimeSpan.FromMinutes(5), "../secrets", TestCt));
    }

    [Fact]
    public async Task GetPreSignedReadUrlAsync_NullPathPrefix_UsesMetadataPrefix()
    {
        using var service = CreateService(o => o.AllowFileUriPresignedUrls = true);
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", pathPrefix: "reports", ct: TestCt);
        var url = await service.GetPreSignedReadUrlAsync(saved.Id, TimeSpan.FromMinutes(5), null, TestCt);
        Assert.StartsWith("file://", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reports", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopyFileAsync_PendingDirectUpload_ThrowsFileNotAvailable()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 100, OriginalFileName = "partial.bin" }, TestCt);
        await Assert.ThrowsAsync<FileNotAvailableException>(() => service.CopyFileAsync(begin.FileId, ct: TestCt));
    }

    [Fact]
    public async Task MoveFileAsync_RelocatesBackingBytes_SameIdUpdatesPathPrefix()
    {
        using var service = CreateService();
        var plain = "move-me-bytes"u8.ToArray();
        var saved = await service.SaveFileAsync(plain, "doc.txt", pathPrefix: "incoming", ct: TestCt);
        var moved = await service.MoveFileAsync(saved.Id, new() { PathPrefix = "archive" }, TestCt);
        Assert.Equal(saved.Id, moved.Id);
        Assert.Equal("archive", moved.PathPrefix);
        Assert.Equal(saved.SourceFileName, moved.SourceFileName);
        Assert.Equal(plain, await service.GetFileAsync(moved.Id, ct: TestCt));
        var keys = await AsDiagnostics(service).ListStorageKeysAsync(null, 1000, TestCt);
        Assert.DoesNotContain(keys, k => k.Contains("incoming", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(keys, k => k.Contains("archive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MoveFileAsync_SamePathPrefix_IsNoOp()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", pathPrefix: "incoming", ct: TestCt);
        var moved = await service.MoveFileAsync(saved.Id, new() { PathPrefix = "incoming" }, TestCt);
        Assert.Equal(saved.Id, moved.Id);
        Assert.Equal("incoming", moved.PathPrefix);
        Assert.Equal(saved.Timestamp, moved.Timestamp);
    }

    [Fact]
    public async Task MoveFileAsync_PendingDirectUpload_ThrowsFileNotAvailable()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 100, OriginalFileName = "partial.bin" }, TestCt);
        await Assert.ThrowsAsync<FileNotAvailableException>(() => service.MoveFileAsync(begin.FileId, new() { PathPrefix = "elsewhere" }, TestCt));
    }

    [Fact]
    public async Task RenameFileAsync_UpdatesOriginalFileName_PhysicalBytesUnchanged()
    {
        using var service = CreateService();
        var plain = "rename-me"u8.ToArray();
        var saved = await service.SaveFileAsync(plain, "old-name.txt", pathPrefix: "docs", ct: TestCt);
        var keysBefore = await AsDiagnostics(service).ListStorageKeysAsync(null, 1000, TestCt);
        var renamed = await service.RenameFileAsync(saved.Id, new() { OriginalFileName = "new-name.txt" }, TestCt);
        Assert.Equal(saved.Id, renamed.Id);
        Assert.Equal("new-name.txt", renamed.OriginalFileName);
        Assert.Equal(saved.PathPrefix, renamed.PathPrefix);
        Assert.Equal(saved.SourceFileName, renamed.SourceFileName);
        Assert.Equal(plain, await service.GetFileAsync(renamed.Id, ct: TestCt));
        var keysAfter = await AsDiagnostics(service).ListStorageKeysAsync(null, 1000, TestCt);
        Assert.Equal(keysBefore.OrderBy(k => k, StringComparer.Ordinal), keysAfter.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public async Task RenameFileAsync_SameName_IsNoOp()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "same.txt", ct: TestCt);
        var renamed = await service.RenameFileAsync(saved.Id, new() { OriginalFileName = "same.txt" }, TestCt);
        Assert.Equal(saved.Timestamp, renamed.Timestamp);
        Assert.Equal("same.txt", renamed.OriginalFileName);
    }

    [Fact]
    public async Task RenameFileAsync_NullOrWhitespaceName_Throws()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.txt", ct: TestCt);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RenameFileAsync(saved.Id, null!, TestCt));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenameFileAsync(saved.Id, new() { OriginalFileName = "   " }, TestCt));
    }

    [Fact]
    public async Task BeginDirectUpload_NoReceiveUri_ThrowsNotSupported()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<NotSupportedException>(() => service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "n.bin" }, TestCt));
    }

    [Fact]
    public async Task BeginDirectUpload_WithReceiveUri_ReturnsPutUrlAndRelativeStorageLocation()
    {
        const string apiBase = "https://tests.invalid";
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = apiBase;
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(
            new() {
                DeclaredMaxSizeBytes = 4096,
                OriginalFileName = "up.bin",
                PathPrefix = "upload-here",
                ContentType = "application/octet-stream"
            }, TestCt);

        Assert.StartsWith($"{apiBase}/FileStorage/direct-upload/", begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.Contains(begin.FileId.ToString("D"), begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.EndsWith("/put", begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(begin.StorageLocation));
        Assert.DoesNotContain('\\', begin.StorageLocation);
        var pending = await service.GetMetadataAsync(begin.FileId, TestCt);
        Assert.Equal(FileAvailability.PendingDirectUpload, pending.Availability);
    }

    [Fact]
    public async Task DirectUpload_ReceivePut_Complete_ReturnsReadableFile()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(
            new() {
                DeclaredMaxSizeBytes = 50_000,
                OriginalFileName = "final.txt",
                ContentType = "text/plain",
                PathPrefix = "du"
            }, TestCt);

        var payload = "hello-direct-upload-plain"u8.ToArray();
        await using (var ms = new MemoryStream(payload))
            await service.ReceiveDirectPutAsync(begin.FileId, ms, TestCt);

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(payload.LongLength, done.OriginalFileSize);
        Assert.Equal(payload, await service.GetFileAsync(begin.FileId, ct: TestCt));
    }

    [Fact]
    public async Task ReceiveDirectPut_InvalidState_Throws()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        var saved = await service.SaveFileAsync("done"u8.ToArray(), "x.txt", ct: TestCt);
        await Assert.ThrowsAsync<ConflictException>(async () => {
            await using var ms = new MemoryStream([1]);
            await service.ReceiveDirectPutAsync(saved.Id, ms, TestCt);
        });
    }

    [Fact]
    public async Task ReceiveDirectPut_NullStream_ThrowsArgumentNull()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        Stream? missing = null;
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await service.ReceiveDirectPutAsync(LyoGuid.CreateV7(), missing!, TestCt);
        });
    }

    [Fact]
    public async Task ListStorageKeys_IncludesUploadedFile_WithForwardSlashShape()
    {
        using var service = CreateService();
        var dx = AsDiagnostics(service);
        await service.SaveFileAsync("k"u8.ToArray(), "keyed.bin", ct: TestCt);
        var keys = await dx.ListStorageKeysAsync(null, 500, TestCt);
        Assert.NotEmpty(keys);
        Assert.All(keys, k => Assert.False(k.Contains('\\', StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ListStorageKeys_Prefix_LimitsSubtree()
    {
        using var service = CreateService();
        await service.SaveFileAsync([1], "under.bin", pathPrefix: "trees/a", ct: TestCt);
        var dx = AsDiagnostics(service);
        var withPrefix = await dx.ListStorageKeysAsync("trees", 50, TestCt);
        Assert.Contains(withPrefix, k => k.StartsWith("trees/", StringComparison.OrdinalIgnoreCase));
        var unrelated = await dx.ListStorageKeysAsync("other-branch", 50, TestCt);
        Assert.Empty(unrelated);
    }

    [Fact]
    public async Task GetPreSignedReadUrl_ResponseOptions_WithAllowFileUri_DoesNotThrow()
    {
        using var service = CreateService(o => o.AllowFileUriPresignedUrls = true);
        var id = (await service.SaveFileAsync([9, 8, 7], "signed.dat", ct: TestCt)).Id;
        var url = await service.GetPreSignedReadUrlAsync(
            id, TimeSpan.FromHours(1), null, new() { ContentDisposition = "attachment; filename=a.dat", ContentType = "application/octet-stream" }, TestCt);

        Assert.StartsWith("file:", url, StringComparison.Ordinal);
    }

    /// <summary>Finalize with no PUT body yields <see cref="FileNotFoundException" />.</summary>
    [Fact]
    public async Task CompleteDirectUpload_MissingBacking_ThrowsFileNotFound()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "missing-body.bin" }, TestCt);
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt));
    }
}