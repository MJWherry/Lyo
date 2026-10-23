using System.IO.Compression;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageArchiveServiceTests : IDisposable
{
    private readonly IOTempService _archiveTemp;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOTempSession _storageTemp = new();

    public FileStorageArchiveServiceTests(ITestOutputHelper output)
    {
        _loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _archiveTemp = new();
    }

    public void Dispose()
    {
        _archiveTemp.Dispose();
        _storageTemp.Dispose();
        _loggerFactory.Dispose();
    }

    private LocalFileStorageService CreateStorage() => new(new() { RootDirectoryPath = _storageTemp.SessionDirectory }, _loggerFactory);

    [Fact]
    public async Task CreateArchiveAsync_TwoFiles_RoundTripsBytesAndNames()
    {
        using var storage = CreateStorage();
        var a = TestData.Create(32);
        var b = TestData.Create(48, TestData.Seed ^ 1);
        var savedA = await storage.SaveFileAsync(a, "alpha.txt", ct: TestCt);
        var savedB = await storage.SaveFileAsync(b, "beta.bin", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var archive = await svc.CreateArchiveAsync([new(savedA.Id), new(savedB.Id)], "Q1 reports.zip", TestCt);
        Assert.Equal("Q1 reports.zip", archive.FileName);
        Assert.Equal("application/zip", archive.ContentType);
        var unzipped = ReadZip(archive.Stream);
        Assert.Equal(a, unzipped["alpha.txt"]);
        Assert.Equal(b, unzipped["beta.bin"]);
        await archive.Stream.DisposeAsync();
    }

    [Fact]
    public async Task CreateArchiveAsync_NestedZipPath_PreservesFolders()
    {
        using var storage = CreateStorage();
        var payload = Encoding.UTF8.GetBytes("page-one");
        var saved = await storage.SaveFileAsync(payload, "page.png", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var archive = await svc.CreateArchiveAsync([new(saved.Id, "Vol. 01/Ch. 001/001")], "chapter.zip", TestCt);
        var unzipped = ReadZip(archive.Stream);
        Assert.Contains("Vol. 01/Ch. 001/001.png", unzipped.Keys);
        Assert.Equal(payload, unzipped["Vol. 01/Ch. 001/001.png"]);
        await archive.Stream.DisposeAsync();
    }

    [Fact]
    public async Task CreateArchiveAsync_OverCount_ThrowsBeforeSpool()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestCt);
        var other = await storage.SaveFileAsync(TestData.Create(8, TestData.Seed ^ 2), "b.txt", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new() { MaxFileCount = 1 }, _loggerFactory.CreateLogger<FileStorageArchiveService>());
        await Assert.ThrowsAsync<FileStorageArchiveLimitException>(() => svc.CreateArchiveAsync([new(saved.Id), new(other.Id)], ct: TestCt));
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    [Fact]
    public async Task CreateArchiveAsync_OverSize_ThrowsBeforeSpool()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(64), "big.bin", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new() { MaxTotalUncompressedBytes = 8 }, _loggerFactory.CreateLogger<FileStorageArchiveService>());
        await Assert.ThrowsAsync<FileStorageArchiveLimitException>(() => svc.CreateArchiveAsync([new(saved.Id)], ct: TestCt));
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    [Fact]
    public async Task CreateArchiveAsync_MissingId_ThrowsFileNotFound()
    {
        using var storage = CreateStorage();
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => svc.CreateArchiveAsync([new(LyoGuid.CreateV7())], ct: TestCt));
        Assert.Contains("available to archive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateArchiveAsync_DeletedFile_SkippedWhenOthersPresent()
    {
        using var storage = CreateStorage();
        var keptBytes = TestData.Create(16);
        var kept = await storage.SaveFileAsync(keptBytes, "keep.txt", ct: TestCt);
        var removed = await storage.SaveFileAsync(TestData.Create(16, TestData.Seed ^ 3), "gone.txt", ct: TestCt);
        Assert.True(await storage.DeleteFileAsync(removed.Id, ct: TestCt));
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var archive = await svc.CreateArchiveAsync([new(removed.Id), new(kept.Id)], "partial.zip", TestCt);
        var unzipped = ReadZip(archive.Stream);
        Assert.Single(unzipped);
        Assert.Equal(keptBytes, unzipped["keep.txt"]);
        await archive.Stream.DisposeAsync();
    }

    [Fact]
    public async Task CreateArchiveAsync_DotDotPath_ThrowsArgumentException()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateArchiveAsync([new(saved.Id, "../escape.txt")], ct: TestCt));
    }

    [Fact]
    public async Task CreateArchiveAsync_DisposeStream_ReleasesTempSession()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestCt);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var archive = await svc.CreateArchiveAsync([new(saved.Id)], ct: TestCt);
        Assert.Equal(1, _archiveTemp.ActiveSessionCount);
        await archive.Stream.DisposeAsync();
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    [Fact]
    public async Task ZipFolder_IncludesDescendantsUnderPrefix()
    {
        var root = _storageTemp.SessionDirectory;
        var metadata = new LocalFileMetadataStore(root);
        using var storage = new LocalFileStorageService(new() { RootDirectoryPath = root }, metadataService: metadata, loggerFactory: _loggerFactory);
        var parent = await storage.SaveFileAsync(Encoding.UTF8.GetBytes("parent"), "index.txt", pathPrefix: "reports", ct: TestCt);
        var child = await storage.SaveFileAsync(Encoding.UTF8.GetBytes("child"), "b.pdf", pathPrefix: "reports/2024", ct: TestCt);
        var outside = await storage.SaveFileAsync(Encoding.UTF8.GetBytes("nope"), "other.txt", pathPrefix: "inbox", ct: TestCt);
        var archives = new FileStorageArchiveService(storage, _archiveTemp, new(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var folder = new FileStorageFolderArchive(metadata, archives);
        var archive = await folder.ZipFolderAsync("reports", "reports.zip", TestCt);
        var unzipped = ReadZip(archive.Stream);
        Assert.Contains("index.txt", unzipped.Keys);
        Assert.Contains("2024/b.pdf", unzipped.Keys);
        Assert.DoesNotContain(unzipped.Keys, name => name.Contains("other"));
        Assert.Equal("parent", Encoding.UTF8.GetString(unzipped["index.txt"]));
        Assert.Equal("child", Encoding.UTF8.GetString(unzipped["2024/b.pdf"]));
        Assert.NotEqual(Guid.Empty, parent.Id);
        Assert.NotEqual(Guid.Empty, child.Id);
        Assert.NotEqual(Guid.Empty, outside.Id);
        await archive.Stream.DisposeAsync();
    }

    private static Dictionary<string, byte[]> ReadZip(Stream zip)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, true);
        var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries) {
            using var s = entry.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            map[entry.FullName] = ms.ToArray();
        }

        return map;
    }
}