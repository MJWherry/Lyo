using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Tests;

/// <summary>Local disk-only features: <see cref="IFileStorageDiagnosticsService" />, <see cref="LocalFileStorageService.CopyFileAsync" />, direct-upload begin/receive/complete.</summary>
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

        // IOTempSession expects RootDirectory to already exist (it does not create the parent chain).
        var ioTempRoot = Path.Combine(Path.GetTempPath(), "lyo-filestorage-tests-io-temp-root-" + Guid.NewGuid().ToString("N"));
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
        var saved = await service.SaveFileAsync(plain, "doc.txt", ct: TestContext.Current.CancellationToken);
        var copy = await service.CopyFileAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        Assert.NotEqual(saved.Id, copy.Id);
        Assert.Equal(saved.OriginalFileHash, copy.OriginalFileHash);
        Assert.Equal(plain, await service.GetFileAsync(copy.Id, TestContext.Current.CancellationToken));
        var copyMeta = await service.GetMetadataAsync(copy.Id, TestContext.Current.CancellationToken);
        Assert.Equal(saved.SourceFileHash, copyMeta.SourceFileHash);
    }

    [Fact]
    public async Task CopyFileAsync_WithPathPrefixOverride_StoresUnderNewPrefix()
    {
        using var service = CreateService();
        var saved = await service.SaveFileAsync("x"u8.ToArray(), "a.bin", pathPrefix: "incoming", ct: TestContext.Current.CancellationToken);
        var copy = await service.CopyFileAsync(saved.Id, new() { PathPrefix = "archive" }, TestContext.Current.CancellationToken);
        var copyMeta = await service.GetMetadataAsync(copy.Id, TestContext.Current.CancellationToken);
        Assert.Equal("archive", copyMeta.PathPrefix);
        Assert.Equal("x"u8.ToArray(), await service.GetFileAsync(copy.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CopyFileAsync_PendingDirectUpload_ThrowsFileNotAvailable()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "Workbench/FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 100, OriginalFileName = "partial.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<FileNotAvailableException>(() => service.CopyFileAsync(begin.FileId, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BeginDirectUpload_NoReceiveUri_ThrowsNotSupported()
    {
        using var service = CreateService();
        await Assert.ThrowsAsync<NotSupportedException>(() => service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "n.bin" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BeginDirectUpload_WithReceiveUri_ReturnsPutUrlAndRelativeStorageLocation()
    {
        const string apiBase = "https://tests.invalid";
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = apiBase;
            o.DirectUploadPutRouteRelativePath = "Workbench/FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(
            new() {
                DeclaredMaxSizeBytes = 4096,
                OriginalFileName = "up.bin",
                PathPrefix = "upload-here",
                ContentType = "application/octet-stream"
            }, TestContext.Current.CancellationToken);

        Assert.StartsWith($"{apiBase}/Workbench/FileStorage/direct-upload/", begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.Contains(begin.FileId.ToString("D"), begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.EndsWith("/put", begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(begin.StorageLocation));
        Assert.DoesNotContain('\\', begin.StorageLocation);
        var pending = await service.GetMetadataAsync(begin.FileId, TestContext.Current.CancellationToken);
        Assert.Equal(FileAvailability.PendingDirectUpload, pending.Availability);
    }

    [Fact]
    public async Task DirectUpload_ReceivePut_Complete_ReturnsReadableFile()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
            o.DirectUploadPutRouteRelativePath = "Workbench/FileStorage/direct-upload";
        });

        var begin = await service.BeginDirectUploadAsync(
            new() {
                DeclaredMaxSizeBytes = 50_000,
                OriginalFileName = "final.txt",
                ContentType = "text/plain",
                PathPrefix = "du"
            }, TestContext.Current.CancellationToken);

        var payload = "hello-direct-upload-plain"u8.ToArray();
        await using (var ms = new MemoryStream(payload))
            await service.ReceiveWorkbenchDirectPutAsync(begin.FileId, ms, TestContext.Current.CancellationToken);

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestContext.Current.CancellationToken);
        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(payload.LongLength, done.OriginalFileSize);
        Assert.Equal(payload, await service.GetFileAsync(begin.FileId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReceiveWorkbenchDirectPut_InvalidState_Throws()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        var saved = await service.SaveFileAsync("done"u8.ToArray(), "x.txt", ct: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await using var ms = new MemoryStream([1]);
            await service.ReceiveWorkbenchDirectPutAsync(saved.Id, ms, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task ReceiveWorkbenchDirectPut_NullStream_ThrowsArgumentNull()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        Stream? missing = null;
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await service.ReceiveWorkbenchDirectPutAsync(Guid.NewGuid(), missing!, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task ListStorageKeys_IncludesUploadedFile_WithForwardSlashShape()
    {
        using var service = CreateService();
        var DX = AsDiagnostics(service);
        await service.SaveFileAsync("k"u8.ToArray(), "keyed.bin", ct: TestContext.Current.CancellationToken);
        var keys = await DX.ListStorageKeysAsync(null, 500, TestContext.Current.CancellationToken);
        Assert.NotEmpty(keys);
        Assert.All(keys, k => Assert.False(k.Contains('\\', StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ListStorageKeys_Prefix_LimitsSubtree()
    {
        using var service = CreateService();
        await service.SaveFileAsync([1], "under.bin", pathPrefix: "trees/a", ct: TestContext.Current.CancellationToken);
        var dx = AsDiagnostics(service);
        var withPrefix = await dx.ListStorageKeysAsync("trees", 50, TestContext.Current.CancellationToken);
        Assert.Contains(withPrefix, k => k.StartsWith("trees/", StringComparison.OrdinalIgnoreCase));
        var unrelated = await dx.ListStorageKeysAsync("other-branch", 50, TestContext.Current.CancellationToken);
        Assert.Empty(unrelated);
    }

    [Fact]
    public async Task GetPreSignedReadUrl_ResponseOptions_WithAllowFileUri_DoesNotThrow()
    {
        using var service = CreateService(o => o.AllowFileUriPresignedUrls = true);
        var id = (await service.SaveFileAsync([9, 8, 7], "signed.dat", ct: TestContext.Current.CancellationToken)).Id;
        var url = await service.GetPreSignedReadUrlAsync(
            id, TimeSpan.FromHours(1), null, new() { ContentDisposition = "attachment; filename=a.dat", ContentType = "application/octet-stream" },
            TestContext.Current.CancellationToken);

        Assert.StartsWith("file:", url, StringComparison.Ordinal);
    }

    /// <summary>Finalize without PUT body yields <see cref="FileNotFoundException" />.</summary>
    [Fact]
    public async Task CompleteDirectUpload_MissingBacking_ThrowsFileNotFound()
    {
        using var service = CreateService(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid/";
        });

        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "missing-body.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.CompleteDirectUploadAsync(begin.FileId, ct: TestContext.Current.CancellationToken));
    }
}