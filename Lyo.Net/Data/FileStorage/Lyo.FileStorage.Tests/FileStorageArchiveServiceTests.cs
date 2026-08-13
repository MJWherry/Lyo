using System.IO.Compression;
using System.Text;
using Lyo.FileStorage.Models;
using Lyo.FileStorage;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageArchiveServiceTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IIOTempSession _storageTemp;
    private readonly IOTempService _archiveTemp;

    public FileStorageArchiveServiceTests(ITestOutputHelper output)
    {
        _loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        _storageTemp = IOTempSession.CreateForTests(nameof(FileStorageArchiveServiceTests), _loggerFactory.CreateLogger<IOTempSession>());
        _archiveTemp = new IOTempService();
    }

    public void Dispose()
    {
        _archiveTemp.Dispose();
        _storageTemp.Dispose();
        _loggerFactory.Dispose();
    }

    private LocalFileStorageService CreateStorage()
        => new(
            new DiskFileStorageOptions { RootDirectoryPath = _storageTemp.SessionDirectory }, _loggerFactory);

    [Fact]
    public async Task CreateArchiveAsync_TwoFiles_RoundTripsBytesAndNames()
    {
        using var storage = CreateStorage();
        var a = TestData.Create(32);
        var b = TestData.Create(48, TestData.Seed ^ 1);
        var savedA = await storage.SaveFileAsync(a, "alpha.txt", ct: TestContext.Current.CancellationToken);
        var savedB = await storage.SaveFileAsync(b, "beta.bin", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new FileStorageArchiveOptions(), _loggerFactory.CreateLogger<FileStorageArchiveService>());

        var archive = await svc.CreateArchiveAsync(
            [new(savedA.Id), new(savedB.Id)], "Q1 reports.zip", TestContext.Current.CancellationToken);
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
        var saved = await storage.SaveFileAsync(payload, "page.png", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new FileStorageArchiveOptions(), _loggerFactory.CreateLogger<FileStorageArchiveService>());

        var archive = await svc.CreateArchiveAsync(
            [new(saved.Id, "Vol. 01/Ch. 001/001")], "chapter.zip", TestContext.Current.CancellationToken);
        var unzipped = ReadZip(archive.Stream);
        Assert.Contains("Vol. 01/Ch. 001/001.png", unzipped.Keys);
        Assert.Equal(payload, unzipped["Vol. 01/Ch. 001/001.png"]);
        await archive.Stream.DisposeAsync();
    }

    [Fact]
    public async Task CreateArchiveAsync_OverCount_ThrowsBeforeSpool()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestContext.Current.CancellationToken);
        var other = await storage.SaveFileAsync(TestData.Create(8, TestData.Seed ^ 2), "b.txt", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new() { MaxFileCount = 1 }, _loggerFactory.CreateLogger<FileStorageArchiveService>());

        await Assert.ThrowsAsync<FileStorageArchiveLimitException>(
            () => svc.CreateArchiveAsync([new(saved.Id), new(other.Id)], ct: TestContext.Current.CancellationToken));
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    [Fact]
    public async Task CreateArchiveAsync_OverSize_ThrowsBeforeSpool()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(64), "big.bin", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(
            storage, _archiveTemp, new() { MaxTotalUncompressedBytes = 8 }, _loggerFactory.CreateLogger<FileStorageArchiveService>());

        await Assert.ThrowsAsync<FileStorageArchiveLimitException>(
            () => svc.CreateArchiveAsync([new(saved.Id)], ct: TestContext.Current.CancellationToken));
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    [Fact]
    public async Task CreateArchiveAsync_MissingId_ThrowsFileNotFound()
    {
        using var storage = CreateStorage();
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new FileStorageArchiveOptions(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => svc.CreateArchiveAsync([new(Guid.NewGuid())], ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateArchiveAsync_DotDotPath_ThrowsArgumentException()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new FileStorageArchiveOptions(), _loggerFactory.CreateLogger<FileStorageArchiveService>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateArchiveAsync([new(saved.Id, "../escape.txt")], ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateArchiveAsync_DisposeStream_ReleasesTempSession()
    {
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(TestData.Create(8), "a.txt", ct: TestContext.Current.CancellationToken);
        var svc = new FileStorageArchiveService(storage, _archiveTemp, new FileStorageArchiveOptions(), _loggerFactory.CreateLogger<FileStorageArchiveService>());
        var archive = await svc.CreateArchiveAsync([new(saved.Id)], ct: TestContext.Current.CancellationToken);
        Assert.Equal(1, _archiveTemp.ActiveSessionCount);
        await archive.Stream.DisposeAsync();
        Assert.Equal(0, _archiveTemp.ActiveSessionCount);
    }

    private static Dictionary<string, byte[]> ReadZip(Stream zip)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
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
