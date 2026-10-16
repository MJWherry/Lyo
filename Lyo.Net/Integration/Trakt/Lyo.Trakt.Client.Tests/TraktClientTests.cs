using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Trakt.Client.Tests;

public sealed class TraktClientTests
{
    [Fact]
    public void AddTraktClient_Options_ResolvesClient()
    {
        var services = new ServiceCollection();
        services.AddTraktClient(Options());
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<TraktClient>());
    }

    [Fact]
    public void Constructor_MissingClientId_Throws()
        => Assert.ThrowsAny<ArgumentException>(() => new TraktClient(new TraktClientOptions()));

    [Fact]
    public async Task SearchMoviesAsync_Query_GetsSearchMovieWithClientHeaders()
    {
        var handler = new RecordingHandler { Respond = _ => Json("[]") };
        var client = Client(handler);
        await client.Search.SearchMoviesAsync("weapons", TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal("api.trakt.tv", call.Uri.Host);
        Assert.EndsWith("/search/movie", call.Uri.AbsolutePath);
        Assert.Contains("query=weapons", call.Uri.Query);
        Assert.Equal("2", call.Headers["trakt-api-version"]);
        Assert.Equal("client-id", call.Headers["trakt-api-key"]);
    }

    [Fact]
    public async Task LookupAsync_ImdbId_GetsSearchImdbKeepingTtPrefix()
    {
        var handler = new RecordingHandler { Respond = _ => Json("[]") };
        var client = Client(handler);
        await client.Search.LookupAsync("imdb", "tt0133093", TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.EndsWith("/search/imdb/tt0133093", call.Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetMovieAsync_Id_GetsMovie()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"title":"The Matrix","ids":{"trakt":1,"imdb":"tt0133093"}}""") };
        var client = Client(handler);
        var movie = await client.Movies.GetAsync("tt0133093", TestCt);

        Assert.EndsWith("/movies/tt0133093", handler.Calls[0].Uri.AbsolutePath);
        Assert.Equal("The Matrix", movie!.Title);
        Assert.Equal("tt0133093", movie.Ids!.Imdb);
    }

    [Fact]
    public async Task TrendingMoviesAsync_WrapperAndPaginationHeaders()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json(
                """[{"watchers":12,"movie":{"title":"The Matrix"}}]""",
                ("X-Pagination-Page", "2"),
                ("X-Pagination-Limit", "10"),
                ("X-Pagination-Page-Count", "4"),
                ("X-Pagination-Item-Count", "40"))
        };
        var client = Client(handler);
        var page = await client.Movies.TrendingAsync(TestCt);

        Assert.EndsWith("/movies/trending", handler.Calls[0].Uri.AbsolutePath);
        Assert.Equal(12, page.Items[0].Watchers);
        Assert.Equal("The Matrix", page.Items[0].Movie!.Title);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.Limit);
        Assert.Equal(4, page.PageCount);
        Assert.Equal(40, page.ItemCount);
    }

    [Fact]
    public async Task GetSeasonAsync_ReturnsEpisodeArray()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""[{"season":1,"number":1,"title":"Pilot"}]""") };
        var client = Client(handler);
        var episodes = await client.Shows.GetSeasonAsync("breaking-bad", 1, TestCt);

        Assert.EndsWith("/shows/breaking-bad/seasons/1", handler.Calls[0].Uri.AbsolutePath);
        Assert.Equal(1, episodes[0].Season);
        Assert.Equal(1, episodes[0].Number);
        Assert.Equal("Pilot", episodes[0].Title);
    }

    [Fact]
    public async Task ExchangeCodeAsync_PostsAuthHostAndReadsRefreshToken()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json("""{"access_token":"access-1","refresh_token":"refresh-1","expires_in":604800,"created_at":1700000000}""")
        };
        var client = Client(handler, secret: "secret", redirectUri: "https://app.example/callback");
        var token = await client.Auth.ExchangeCodeAsync("auth-code", TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("auth.trakt.tv", call.Uri.Host);
        Assert.EndsWith("/oauth/token", call.Uri.AbsolutePath);
        Assert.Contains("\"grant_type\":\"authorization_code\"", call.Body);
        Assert.Contains("\"code\":\"auth-code\"", call.Body);
        Assert.Equal("refresh-1", token.RefreshToken);
        Assert.Equal("access-1", token.AccessToken);
    }

    [Fact]
    public async Task PollDeviceTokenAsync_PendingAndSlowDown_ReturnsStatusWithoutThrowing()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.BadRequest, (HttpStatusCode)429]);
        var handler = new RecordingHandler {
            Respond = _ => new HttpResponseMessage(statuses.Dequeue()) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }
        };
        var services = new ServiceCollection();
        services.AddTraktClient(Options(secret: "secret")).ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TraktClient>();

        var pending = await client.Auth.PollDeviceTokenAsync("device-code", TestCt);
        var slow = await client.Auth.PollDeviceTokenAsync("device-code", TestCt);

        Assert.Equal(TraktDevicePollStatus.Pending, pending.Status);
        Assert.Equal(TraktDevicePollStatus.SlowDown, slow.Status);
        Assert.Equal(2, handler.Calls.Count);
        Assert.All(handler.Calls, call => {
            Assert.Equal("auth.trakt.tv", call.Uri.Host);
            Assert.EndsWith("/oauth/device/token", call.Uri.AbsolutePath);
        });
    }

    private static TraktClient Client(HttpMessageHandler handler, string? secret = null, string? redirectUri = null)
        => new(Options(secret, redirectUri), httpClient: new HttpClient(handler));

    private static TraktClientOptions Options(string? secret = null, string? redirectUri = null)
        => new() {
            ClientId = "client-id",
            ClientSecret = secret,
            RedirectUri = redirectUri,
            BaseUrl = "https://api.trakt.tv/"
        };

    private static HttpResponseMessage Json(string json, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        foreach (var (name, value) in headers)
            response.Headers.TryAddWithoutValidation(name, value);

        return response;
    }
}
