using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.FileStorage.Models;
using Lyo.Common.Metadata.Records;
using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Query.Models.Common.Request;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStorageHttpTreeSourceTests
{
    [Fact]
    public void ToEntryAndToPathNode_RoundTripPresenceFlags()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        foreach (var presence in new[] { FileStoragePresence.None, FileStoragePresence.Store, FileStoragePresence.Physical, FileStoragePresence.Both }) {
            var dto = new FileStorageFolderEntryDto("keep.txt", false, "reports", fileId, presence, "keep.txt", 4, "reports/" + fileId.ToString("N"));
            var entry = FileStorageHttpTreeSource.ToEntry(dto);
            Assert.Equal(presence.ToString(), entry.Properties!["Presence"]);
            var node = FileStorageHttpTreeSource.ToPathNode(entry);
            Assert.Equal(presence, node.Presence);
            Assert.Equal(fileId, node.FileId);
            Assert.Equal("reports", node.PathPrefix);
            Assert.Equal("reports/" + fileId.ToString("N"), node.PhysicalKey);
            Assert.Equal("reports/" + fileId.ToString("N"), entry.Properties!["PhysicalKey"]);
        }
    }

    [Fact]
    public void ToEntry_Directory_OmitsFileChips()
    {
        var dto = new FileStorageFolderEntryDto("report", true, "report", null, FileStoragePresence.Physical, null, 0, null);
        var entry = FileStorageHttpTreeSource.ToEntry(dto);
        Assert.Equal(nameof(FileStoragePresence.Physical), entry.Properties!["Presence"]);
        Assert.False(entry.Properties.ContainsKey("FileId"));
        Assert.False(entry.Properties.ContainsKey("OriginalFileName"));
        Assert.False(entry.Properties.ContainsKey("PhysicalKey"));
    }

    [Fact]
    public void ToPathNode_PhysicalOnly_KeepsPhysicalKeyWithoutCatalog()
    {
        var dto = new FileStorageFolderEntryDto("orphan.bin", false, null, null, FileStoragePresence.Physical, null, 0, "orphan.bin");
        var entry = FileStorageHttpTreeSource.ToEntry(dto);
        var node = FileStorageHttpTreeSource.ToPathNode(entry);
        Assert.Equal(FileStoragePresence.Physical, node.Presence);
        Assert.Null(node.FileId);
        Assert.Equal("orphan.bin", node.PhysicalKey);
    }

    [Fact]
    public void PresenceEnum_HasNoBlobOrCloudNames()
    {
        var names = Enum.GetNames<FileStoragePresence>();
        Assert.Contains(nameof(FileStoragePresence.Store), names);
        Assert.Contains(nameof(FileStoragePresence.Physical), names);
        Assert.Contains(nameof(FileStoragePresence.Both), names);
        Assert.DoesNotContain(names, n => n.Contains("Blob", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Cloud", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PrefixFromPath_RootAndFolder()
    {
        Assert.Null(FileStorageHttpTreeSource.PrefixFromPath("/"));
        Assert.Equal("reports/2024", FileStorageHttpTreeSource.PrefixFromPath("/reports/2024"));
    }

    [Fact]
    public void DirectoryNode_DoesNotNeedAPopulatedRoot()
    {
        var root = FileStorageHttpTreeSource.DirectoryNode(null);
        Assert.True(root.IsDirectory);
        Assert.Null(root.PathPrefix);
        Assert.Equal(FileStoragePathTreeBuilder.RootDisplayName, root.Name);
        Assert.Equal("/", FileStorageHttpTreeSource.DirectoryVfsPath(null));

        var nested = FileStorageHttpTreeSource.DirectoryNode("reports/2024");
        Assert.True(nested.IsDirectory);
        Assert.Equal("reports/2024", nested.PathPrefix);
        Assert.Equal("2024", nested.Name);
        Assert.Equal("/reports/2024", FileStorageHttpTreeSource.DirectoryVfsPath("reports/2024"));
    }

    [Fact]
    public async Task ListChildren_Blob_RequestsFolder()
    {
        var api = new RecordingApi();
        var source = new FileStorageHttpTreeSource(api, static path => path);
        await source.ListChildrenAsync("/", TestCt);
        var uri = Assert.Single(api.Uris);
        Assert.Contains("files/folder", uri);
        Assert.DoesNotContain("files/tags/group", uri);
    }

    [Fact]
    public async Task ListChildren_GroupBy_RequestsTagGroup()
    {
        var api = new RecordingApi();
        var source = new FileStorageHttpTreeSource(api, static path => path);
        source.UseTagStructure(["client", "imported:year"]);
        await source.ListChildrenAsync("/clienta/2026", TestCt);
        var grouped = Uri.UnescapeDataString(Assert.Single(api.Uris));
        Assert.Contains("files/tags/group", grouped);
        Assert.DoesNotContain("files/folder", grouped);
        Assert.Contains("groupBy=client", grouped);
        Assert.Contains("groupBy=imported:year", grouped);
        Assert.Contains("path=clienta/2026", grouped);

        api.Uris.Clear();
        source.UseBlobStructure();
        await source.ListChildrenAsync("/clienta/2026", TestCt);
        var blob = Assert.Single(api.Uris);
        Assert.Contains("files/folder", blob);
        Assert.DoesNotContain("files/tags/group", blob);
    }

    private sealed class RecordingApi : IApiClient
    {
        public List<string> Uris { get; } = [];

        public void Dispose() { }

        public JsonSerializerOptions GetSerializerOptions() => new();

        public HttpClient GetClient() => throw new NotImplementedException();

        public ILyoHttpSession Session { get; } = new LyoHttpSession();

        public LyoHttpClientHooks Hooks { get; } = new();

        public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

        public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

        public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            Uris.Add(uri);
            if (typeof(TResult) == typeof(FileStorageFolderListResponse))
                return Task.FromResult((TResult?)(object)new FileStorageFolderListResponse([], false));

            return Task.FromResult(default(TResult));
        }

        public Task<TResult?> GetAsAsync<TRequest, TResult>(
            string uri, TRequest? query = default, string? enumerableDelimiter = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

        public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<string> DownloadToFileAsync(
            string uri, string destinationPath, IProgress<LyoHttpDownloadProgress>? progress = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostFileAsAsync<TResult>(
            string uri, Stream stream, FileTypeInfo fileType, string? fileName = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostFileAsAsync<TResult>(
            string uri, byte[] data, FileTypeInfo fileType, string? fileName = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(uri);

        public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

        public Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(route);

        public Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(route);

        public Task<TResult?> QuerySqlAsync<TResult>(string route, QueryRequestBase request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException(route);
    }
}
