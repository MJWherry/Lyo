using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

/// <summary>Client-specified <see cref="FileStoreResult.Charset" /> is stored as given. No conversion.</summary>
public sealed class LocalFileStorageCharsetMetadataTests
{
    private static LocalFileStorageTestScope CreateDirectUploadScope()
        => LocalFileStorageTestScope.Create(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.DirectUploadPutRouteRelativePath = "FileStorage/direct-upload";
            return o;
        });

    [Fact]
    public async Task SaveFileAsync_WithCharset_StoresUnchanged()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("hello"u8.ToArray(), "notes.txt", contentType: "text/plain", charset: "windows-1252", ct: TestContext.Current.CancellationToken);
        Assert.Equal("windows-1252", saved.Charset);
        var meta = await scope.Storage.GetMetadataAsync(saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal("windows-1252", meta.Charset);
    }

    [Fact]
    public async Task SaveFileAsync_TrimsCharset_AndWhitespaceBecomesNull()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var trimmed = await scope.Storage.SaveFileAsync("a"u8.ToArray(), "a.txt", charset: "  utf-8  ", ct: TestContext.Current.CancellationToken);
        Assert.Equal("utf-8", trimmed.Charset);
        var omitted = await scope.Storage.SaveFileAsync("b"u8.ToArray(), "b.txt", ct: TestContext.Current.CancellationToken);
        Assert.Null(omitted.Charset);
        var blank = await scope.Storage.SaveFileAsync("c"u8.ToArray(), "c.txt", charset: " \t ", ct: TestContext.Current.CancellationToken);
        Assert.Null(blank.Charset);
    }

    [Fact]
    public async Task SaveFileAsync_CharsetLongerThanMax_ThrowsArgumentOutsideRangeException()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var tooLong = new string('x', FileStoreResult.MaxCharsetLength + 1);
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => scope.Storage.SaveFileAsync("a"u8.ToArray(), "a.txt", charset: tooLong, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CopyFileAsync_PreservesCharset()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("copy-me"u8.ToArray(), "src.txt", charset: "iso-8859-1", ct: TestContext.Current.CancellationToken);
        var copy = await scope.Storage.CopyFileAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        Assert.Equal("iso-8859-1", copy.Charset);
        var meta = await scope.Storage.GetMetadataAsync(copy.Id, TestContext.Current.CancellationToken);
        Assert.Equal("iso-8859-1", meta.Charset);
    }

    [Fact]
    public async Task DirectUpload_BeginAndComplete_KeepsCharset()
    {
        using var scope = CreateDirectUploadScope();
        var begin = await scope.Storage.BeginDirectUploadAsync(
            new() {
                DeclaredMaxSizeBytes = 50_000,
                OriginalFileName = "final.txt",
                ContentType = "text/plain",
                Charset = "utf-8",
                PathPrefix = "du"
            }, TestContext.Current.CancellationToken);

        var pending = await scope.Storage.GetMetadataAsync(begin.FileId, TestContext.Current.CancellationToken);
        Assert.Equal("utf-8", pending.Charset);

        var payload = "hello-direct-upload-plain"u8.ToArray();
        await using (var ms = new MemoryStream(payload))
            await scope.Storage.ReceiveDirectPutAsync(begin.FileId, ms, TestContext.Current.CancellationToken);

        var done = await scope.Storage.CompleteDirectUploadAsync(begin.FileId, ct: TestContext.Current.CancellationToken);
        Assert.Equal("utf-8", done.Charset);
    }

    [Fact]
    public async Task LocalMultipart_BeginComplete_KeepsCharset()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var sessions = new InMemoryMultipartUploadSessionStore();
        var multipart = new LocalMultipartUploadService(scope.Storage, sessions, scope.Options);
        var begin = await multipart.BeginAsync(new() { PartSizeBytes = 16 * 1024, Charset = "utf-16" }, TestContext.Current.CancellationToken);
        var payload = "hello multipart"u8.ToArray();
        await multipart.UploadPartAsync(begin.SessionId, 1, new MemoryStream(payload), TestContext.Current.CancellationToken);
        var meta = await multipart.CompleteAsync(
            new() { SessionId = begin.SessionId, Parts = [new() { PartNumber = 1, ETagOrBlockId = "n/a" }] }, TestContext.Current.CancellationToken);
        Assert.Equal("utf-16", meta.Charset);
    }
}
