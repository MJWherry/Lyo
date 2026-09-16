using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Query.Models.Common.Request;
using Lyo.Recipe.Client;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Models.Request;
using Lyo.Recipe.Models.Response;

namespace Lyo.Recipe.Client.Tests;

[Trait("Category", "Fast")]
public sealed class RecipeClientTests
{
    [Fact]
    public void Validate_SchemeInPrefix_Throws()
    {
        var options = new RecipeClientOptions { RoutePrefix = "http://localhost" };
        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public async Task ReplaceIngredientLinesAsync_PostsReplaceRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var lines = new List<RecipeIngredientLineReq> { new() { IngredientText = "salt", SortOrder = 0 } };
        await client.ReplaceIngredientLinesAsync(id, lines, TestCt);
        Assert.Equal($"Recipe/Recipes/{id:D}/IngredientLines", api.LastUri);
        Assert.Same(lines, api.LastBody);
    }

    [Fact]
    public async Task ReplaceStepsAsync_WhenPrefix_PostsPrefixedRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api, new RecipeClientOptions { RoutePrefix = "api" });
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var steps = new List<RecipeStepReq> { new() { Instruction = "Mix", SortOrder = 0 } };
        await client.ReplaceStepsAsync(id, steps, TestCt);
        Assert.Equal($"api/Recipe/Recipes/{id:D}/Steps", api.LastUri);
        Assert.Same(steps, api.LastBody);
    }

    [Fact]
    public async Task AddNoteAsync_PostsNotesRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var req = new RecipeNoteCreateReq { Content = "Tried this twice" };
        await client.AddNoteAsync(Constants.Rest.Recipe.Recipes, id, req, TestCt);
        Assert.Equal($"Recipe/Recipes/{id:D}/Notes", api.LastUri);
        Assert.Same(req, api.LastBody);
    }

    [Fact]
    public async Task ListUnitsAsync_GetsUnitsRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api);
        await client.ListUnitsAsync(TestCt);
        Assert.Equal("Recipe/Units", api.LastUri);
    }

    [Fact]
    public async Task ListIngredientUsageAsync_GetsUsageRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        await client.ListIngredientUsageAsync(id, TestCt);
        Assert.Equal($"Recipe/Ingredients/{id:D}/Usage", api.LastUri);
    }

    [Fact]
    public async Task UploadAttachmentAsync_PostsUploadRoute()
    {
        var api = new RecordingApiClient();
        var client = new RecipeClient(api);
        var entityId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        await using var stream = new MemoryStream([1, 2, 3]);
        await client.UploadAttachmentAsync(RecipeEntityTypes.Recipe, entityId, stream, "scan.bin", RecipeAttachmentRole.Scan, "front", false, TestCt);
        Assert.Equal("Recipe/Attachments/upload", api.LastUri);
    }
}

internal sealed class RecordingApiClient : IApiClient
{
    private readonly HttpClient _http = new();

    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public object? NextResult { get; init; }

    public void Dispose() => _http.Dispose();

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => _http;

    public ILyoHttpSession Session { get; } = new LyoHttpSession();

    public LyoHttpClientHooks Hooks { get; } = new();

    public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

    public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(NextResult is TResult next ? next : default(TResult)!);
    }

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

    public Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QuerySqlAsync<TResult>(string route, QueryRequestBase request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);
}
