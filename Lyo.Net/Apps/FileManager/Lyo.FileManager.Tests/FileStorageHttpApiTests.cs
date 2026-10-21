using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Net;

namespace Lyo.FileManager.Tests;

public class FileStorageHttpApiTests
{
    [Fact]
    public async Task Authenticate_Bearer_SendsAuthorization()
    {
        var handler = new RecordingHandler(_ => Json("{\"isHealthy\":true}"));
        using var http = Client(handler);
        var server = Remote();
        server.AuthKind = FileManagerAuthKind.Bearer;
        server.Secret = "tok";
        using var api = new FileStorageHttpApi(server, http);
        await api.CheckHealthAsync(TestCt);
        var authorization = handler.Requests[0].Headers.Authorization;
        Assert.NotNull(authorization);
        Assert.Equal("Bearer", authorization.Scheme);
        Assert.Equal("tok", authorization.Parameter);
    }

    [Fact]
    public async Task Authenticate_ApiKey_SendsXApiKey()
    {
        var handler = new RecordingHandler(_ => Json("{\"isHealthy\":true}"));
        using var http = Client(handler);
        var server = Remote();
        server.AuthKind = FileManagerAuthKind.ApiKey;
        server.Secret = "secret";
        using var api = new FileStorageHttpApi(server, http);
        await api.CheckHealthAsync(TestCt);
        Assert.True(handler.Requests[0].Headers.TryGetValues(LyoHttpHeaders.ApiKey, out var values));
        Assert.Equal("secret", values!.Single());
    }

    [Fact]
    public async Task Query_PostsQueryProject_WithNameAndPrefix()
    {
        var handler = new RecordingHandler(_ => Json("{\"items\":[]}"));
        using var http = Client(handler);
        using var api = new FileStorageHttpApi(Remote(), http);
        await api.QueryAsync("report", "docs", TestCt);
        var request = handler.Requests[0];
        Assert.EndsWith("/FileStorage/FileMetadata/QueryProject", request.Uri.AbsolutePath);
        var body = request.Body;
        Assert.Contains("OriginalFileName", body, StringComparison.Ordinal);
        Assert.Contains("report", body, StringComparison.Ordinal);
        Assert.Contains("docs", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upload_LargeFile_BeginsMultipart_AndCompletes()
    {
        var handler = new RecordingHandler(request => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/capabilities", StringComparison.Ordinal))
                return Json(CapabilitiesJson(true));

            if (path.EndsWith("/multipart/begin", StringComparison.Ordinal)) {
                return Json(
                    """
                    {
                      "sessionId": "11111111-1111-1111-1111-111111111111",
                      "targetFileId": "22222222-2222-2222-2222-222222222222",
                      "partSizeBytes": 8,
                      "expiresUtc": "2030-01-01T00:00:00Z",
                      "providerKind": "Local"
                    }
                    """);
            }

            if (path.Contains("/part-url", StringComparison.Ordinal))
                return Json("{\"partNumber\":1,\"presignedPutUrl\":\"http://uploads.example/part\",\"httpMethod\":\"PUT\"}");

            if (request.RequestUri.Host == "uploads.example")
                return new(HttpStatusCode.OK) { Headers = { ETag = new("\"etag-1\"") } };

            return Json("{}");
        });

        using var http = Client(handler);
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-part-" + LyoGuid.CreateV7().ToString("N"));
        await File.WriteAllTextAsync(path, "hi", TestCt);
        try {
            using var api = new FileStorageHttpApi(Remote(), http);
            await api.UploadAsync(path, "docs", new() { MultipartThresholdBytes = 1 }, TestCt);
        }
        finally {
            File.Delete(path);
        }

        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/capabilities", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/multipart/begin", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/multipart/complete", StringComparison.Ordinal));
        var complete = handler.Requests.Last(request => request.Uri.AbsolutePath.EndsWith("/multipart/complete")).Body;
        Assert.Contains("etag-1", complete, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upload_LargeFile_WithoutPresignedParts_UsesSaveStream()
    {
        var handler = new RecordingHandler(request => {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/capabilities", StringComparison.Ordinal))
                return Json(CapabilitiesJson(false));

            return Json("{}");
        });

        using var http = Client(handler);
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-stream-" + LyoGuid.CreateV7().ToString("N"));
        await File.WriteAllTextAsync(path, "hi", TestCt);
        try {
            using var api = new FileStorageHttpApi(Remote(), http);
            await api.UploadAsync(path, "docs", new() { MultipartThresholdBytes = 1 }, TestCt);
        }
        finally {
            File.Delete(path);
        }

        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/capabilities", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.Contains("/files/save-stream", StringComparison.Ordinal));
        Assert.DoesNotContain(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/multipart/begin", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Download_WritesTheGivenPath()
    {
        var handler = new RecordingHandler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent("bytes"u8.ToArray()) });
        using var http = Client(handler);
        var dest = Path.Combine(Path.GetTempPath(), "lyo-fm-dl-" + LyoGuid.CreateV7().ToString("N"));
        try {
            using var api = new FileStorageHttpApi(Remote(), http);
            var id = LyoGuid.CreateV7();
            await api.DownloadAsync(id, dest, TestCt);
            Assert.Equal("bytes", await File.ReadAllTextAsync(dest, TestCt));
            Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith($"/files/{id:D}/download", StringComparison.Ordinal));
        }
        finally {
            if (File.Exists(dest))
                File.Delete(dest);
        }
    }

    [Fact]
    public async Task DownloadMany_RequestsArchiveWithThoseIds()
    {
        var handler = new RecordingHandler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent("PK"u8.ToArray()) });
        using var http = Client(handler);
        var dest = Path.Combine(Path.GetTempPath(), "lyo-fm-zip-" + LyoGuid.CreateV7().ToString("N"));
        var first = LyoGuid.CreateV7();
        var second = LyoGuid.CreateV7();
        try {
            using var api = new FileStorageHttpApi(Remote(), http);
            await api.DownloadArchiveAsync([first, second], dest, TestCt);
            var query = handler.Requests[0].Uri.Query;
            Assert.Contains(first.ToString("D"), query, StringComparison.Ordinal);
            Assert.Contains(second.ToString("D"), query, StringComparison.Ordinal);
            Assert.Contains("/files/archive", handler.Requests[0].Uri.AbsolutePath, StringComparison.Ordinal);
        }
        finally {
            if (File.Exists(dest))
                File.Delete(dest);
        }
    }

    [Fact]
    public async Task AccessLink_PostsExpiryAndMaxDownloads()
    {
        var handler = new RecordingHandler(_ => Json(
            """
            {
              "linkId": "11111111-1111-1111-1111-111111111111",
              "token": "tok",
              "downloadUrl": "FileStorage/files/access/tok/download",
              "presignedReadUrl": "",
              "createdUtc": "2030-01-01T00:00:00Z"
            }
            """));

        using var http = Client(handler);
        using var api = new FileStorageHttpApi(Remote(), http);
        var when = new DateTime(2030, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var url = await api.CreateAccessLinkAsync(LyoGuid.CreateV7(), when, 3, TestCt);
        var body = handler.Requests[0].Body;
        Assert.Contains("2030-01-02", body, StringComparison.Ordinal);
        Assert.Contains("3", body, StringComparison.Ordinal);
        Assert.Contains("/files/access/tok/download", url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PhysicalRoutes_PostMoveRenameDelete()
    {
        var handler = new RecordingHandler(_ => Json("{}"));
        using var http = Client(handler);
        using var api = new FileStorageHttpApi(Remote(), http);
        await api.MovePhysicalAsync("loose/a", "kept", TestCt);
        await api.RenamePhysicalAsync("loose/a", "b", TestCt);
        await api.DeletePhysicalAsync("loose/a", TestCt);
        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/files/physical/move", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Uri.AbsolutePath.EndsWith("/files/physical/rename", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Delete && request.Uri.AbsolutePath.EndsWith("/files/physical", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RotateSelection_PostsRotateDeks()
    {
        var handler = new RecordingHandler(_ => Json("{\"totalFilesFound\":1,\"successfullyMigrated\":1,\"failed\":0,\"failedFileIds\":[],\"errors\":[]}"));
        using var http = Client(handler);
        using var api = new FileStorageHttpApi(Remote(), http);
        var id = LyoGuid.CreateV7();
        await api.RotateAsync([id], "kek", null, TestCt);
        Assert.EndsWith("/files/rotate-deks", handler.Requests[0].Uri.AbsolutePath, StringComparison.Ordinal);
        var body = handler.Requests[0].Body;
        Assert.Contains(id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kek", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrateKeys_PostsMigrateDeks()
    {
        var handler = new RecordingHandler(_ => Json("{\"totalFilesFound\":0,\"successfullyMigrated\":0,\"failed\":0,\"failedFileIds\":[],\"errors\":[]}"));
        using var http = Client(handler);
        using var api = new FileStorageHttpApi(Remote(), http);
        await api.MigrateAsync("old", null, "new", null, TestCt);
        Assert.EndsWith("/files/migrate-deks", handler.Requests[0].Uri.AbsolutePath, StringComparison.Ordinal);
        var body = handler.Requests[0].Body;
        Assert.Contains("old", body, StringComparison.Ordinal);
        Assert.Contains("new", body, StringComparison.Ordinal);
    }

    private static string CapabilitiesJson(bool presignedMultipartParts)
        => $$"""
             {
               "presignedRead": true,
               "directUpload": true,
               "multipart": true,
               "presignedMultipartParts": {{(presignedMultipartParts ? "true" : "false")}},
               "copy": true,
               "move": true,
               "physical": true,
               "encryption": true,
               "archive": true,
               "accessLinks": true
             }
             """;

    private static FileManagerServer Remote() => new() { Kind = FileManagerServerKind.Remote, BaseUrl = "http://localhost:9", Route = "FileStorage" };

    private static HttpClient Client(RecordingHandler handler) => new(handler) { BaseAddress = new("http://localhost:9/") };

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Captured> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(new(request.Method, request.RequestUri!, request.Headers, body));
            return respond(request);
        }
    }

    private sealed record Captured(HttpMethod Method, Uri Uri, HttpRequestHeaders Headers, string Body);
}