using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Query.Models.Common.Request;
using Lyo.Story.Enums;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Client.Tests;

[Trait("Category", "Fast")]
public sealed class StoryClientTests
{
    [Fact]
    public void Validate_SchemeInPrefix_Throws()
    {
        var options = new StoryClientOptions { RoutePrefix = "http://localhost" };
        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public async Task SaveGraphAsync_PostsGraphRoute()
    {
        var api = new RecordingApiClient();
        var client = new StoryClient(api);
        var req = new StoryGraphReq { Title = "The Heist", Slug = "the-heist" };
        await client.SaveGraphAsync(req, TestCt);
        Assert.Equal("Story/graph", api.LastUri);
        Assert.Same(req, api.LastBody);
    }

    [Fact]
    public async Task ChooseAsync_WhenPrefix_PostsPrefixedRoute()
    {
        var api = new RecordingApiClient();
        var client = new StoryClient(api, new() { RoutePrefix = "api" });
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var req = new StoryChooseReq { ChoiceKey = "smash" };
        await client.ChooseAsync(id, req, TestCt);
        Assert.Equal($"api/Story/playthrough/{id:D}/choose", api.LastUri);
        Assert.Same(req, api.LastBody);
    }

    [Fact]
    public async Task DeleteAsync_DeletesStoryRow()
    {
        var api = new RecordingApiClient();
        var client = new StoryClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        await client.DeleteAsync(id, TestCt);
        Assert.Equal($"Story/Stories/{id:D}", api.LastUri);
    }

    [Fact]
    public async Task SetVariablesAsync_PostsVariablesRoute()
    {
        var api = new RecordingApiClient();
        var client = new StoryClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        await client.SetVariablesAsync(id, new() { Values = new() { ["game.has.potion"] = "True" } }, TestCt);
        Assert.Equal($"Story/playthrough/{id:D}/variables", api.LastUri);
        var body = Assert.IsType<StorySetVariablesReq>(api.LastBody);
        Assert.Equal("True", body.Values["game.has.potion"]);
    }

    [Fact]
    public async Task StoryClientStore_TakeChoice_PostsChooseAndMapsState()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var api = new RecordingApiClient {
            NextResult = new StoryPlayStateRes {
                Playthrough = new() {
                    Id = id,
                    CurrentNodeKey = "alarm_cutscene",
                    Status = StoryPlaythroughStatus.InProgress,
                    Variables = [new() { VariableKey = "alarm", Value = "True" }]
                },
                CurrentNode = new() { Key = "alarm_cutscene", PresentationKind = StoryPresentationKind.Cutscene, MediaRef = "media:heist-alarm" },
                Moves = [new() { Kind = StoryMoveKind.Advance, Enabled = true }]
            }
        };

        var store = new StoryClientStore(new StoryClient(api));
        var state = await store.TakeChoiceAsync(id, "smash", TestCt);
        Assert.Equal($"Story/playthrough/{id:D}/choose", api.LastUri);
        Assert.Equal("smash", Assert.IsType<StoryChooseReq>(api.LastBody).ChoiceKey);
        Assert.Equal("alarm_cutscene", state.Playthrough.CurrentNodeKey);
        Assert.Equal("True", Assert.Single(state.Playthrough.Variables).Value);
        Assert.Equal("media:heist-alarm", state.CurrentNode?.MediaRef);
        Assert.Equal(StoryMoveKind.Advance, Assert.Single(state.Moves).Kind);
    }

    [Fact]
    public async Task StoryClientStore_ListStories_PostsTenantAndGame()
    {
        var tenant = Guid.Parse("0199a8a0-0000-7000-8000-0000000000a1");
        var api = new RecordingApiClient {
            NextResult = new[] {
                new StoryGraphRes {
                    Id = Guid.Parse("0199a8a0-0000-7000-8000-0000000000b2"),
                    Title = "Intro",
                    Slug = "intro",
                    TenantId = tenant,
                    GameKey = "rpg"
                }
            }
        };

        var store = new StoryClientStore(new StoryClient(api));
        var stories = await store.ListStoriesAsync(tenant, "rpg", TestCt);
        Assert.Equal("Story/graph/list", api.LastUri);
        var body = Assert.IsType<StoryListReq>(api.LastBody);
        Assert.Equal(tenant, body.TenantId);
        Assert.Equal("rpg", body.GameKey);
        var story = Assert.Single(stories);
        Assert.Equal(tenant, story.TenantId);
        Assert.Equal("rpg", story.GameKey);
    }

    [Fact]
    public async Task StoryClientStore_SaveStoryGraph_PostsGraphAndTakesServerId()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var api = new RecordingApiClient { NextResult = new StoryGraphRes { Id = id, Title = "The Heist", Slug = "the-heist" } };
        var store = new StoryClientStore(new StoryClient(api));
        var story = StorySamples.TheHeist();
        await store.SaveStoryGraphAsync(story, TestCt);
        Assert.Equal("Story/graph", api.LastUri);
        var body = Assert.IsType<StoryGraphReq>(api.LastBody);
        Assert.Equal("vault_door", body.StartNodeKey);
        Assert.Equal(4, body.Nodes.Single(n => n.Key == "vault_door").Choices.Single(c => c.Key == "pick_lock").Effects.Count);
        Assert.Equal(id, story.Id);
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
        return Task.FromResult(NextResult is TResult next ? next : default!);
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
        => throw new NotImplementedException(uri);

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
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

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