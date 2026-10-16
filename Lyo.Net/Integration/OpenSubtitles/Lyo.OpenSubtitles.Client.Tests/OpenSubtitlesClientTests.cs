using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.OpenSubtitles.Client.Tests;

public sealed class OpenSubtitlesClientTests
{
    [Fact]
    public void AddOpenSubtitlesClient_Options_ResolvesClient()
    {
        var services = new ServiceCollection();
        services.AddOpenSubtitlesClient(Options());
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<OpenSubtitlesClient>());
    }

    [Fact]
    public void Constructor_MissingApiKeyOrUserAgent_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => new OpenSubtitlesClient(new OpenSubtitlesClientOptions { ApplicationUserAgent = "App v1" }));
        Assert.ThrowsAny<ArgumentException>(() => new OpenSubtitlesClient(new OpenSubtitlesClientOptions { ApiKey = "key" }));
    }

    [Fact]
    public async Task LoginAsync_ReturnsHostnameAndToken_NextSearchUsesThatHost()
    {
        var handler = new RecordingHandler {
            Respond = call => call.Uri.AbsolutePath.EndsWith("/login", StringComparison.Ordinal)
                ? Json("""{"token":"jwt-1","base_url":"vip-api.opensubtitles.com"}""")
                : Json("""{"data":[]}""")
        };
        var client = Client(handler);
        var session = await client.Auth.LoginAsync("user", "secret", TestCt);
        Assert.Equal("jwt-1", session.Token);
        Assert.Equal("vip-api.opensubtitles.com", session.Host);

        await client.Subtitles.SearchAsync(new SubtitleSearchQuery(), session, TestCt);
        var search = handler.Calls[^1];
        Assert.Equal(HttpMethod.Get, search.Method);
        Assert.Equal("vip-api.opensubtitles.com", search.Uri.Host);
        Assert.EndsWith("/api/v1/subtitles", search.Uri.AbsolutePath);
    }

    [Fact]
    public async Task SearchAsync_ImdbAndLanguages_UsesNumericIdAndOneLanguagesValue()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"data":[]}""") };
        var client = Client(handler);
        await client.Subtitles.SearchAsync(new SubtitleSearchQuery { ImdbId = "tt0133093", Languages = ["en", "fr"] }, ct: TestCt);

        var query = handler.Calls[0].Uri.Query.TrimStart('?').Split('&');
        Assert.Contains("imdb_id=133093", query);
        var languages = query.Where(part => part.StartsWith("languages=", StringComparison.Ordinal)).ToArray();
        Assert.Single(languages);
        Assert.Equal("en,fr", Uri.UnescapeDataString(languages[0]["languages=".Length..]));
        Assert.DoesNotContain(query, part => part.Contains("tt0133093", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DownloadAsync_FileId_PostsFileIdAndReturnsLink()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json("""{"link":"https://dl.opensubtitles.com/download/x","file_name":"a.srt","requests":2,"remaining":3}""")
        };
        var client = Client(handler);
        var session = new OpenSubtitlesSession("jwt-1", "api.opensubtitles.com");
        var download = await client.Subtitles.DownloadAsync(11047023, session, TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.EndsWith("/api/v1/download", call.Uri.AbsolutePath);
        Assert.Contains("\"file_id\":11047023", call.Body);
        Assert.Equal("Bearer jwt-1", call.Headers["Authorization"]);
        Assert.Equal("https://dl.opensubtitles.com/download/x", download.Link);
        Assert.Equal(3, download.Remaining);
    }

    [Fact]
    public async Task GuessAsync_Filename_GetsUtilitiesGuessit()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"title":"The Matrix","type":"movie"}""") };
        var client = Client(handler);
        await client.Utilities.GuessAsync("The.Matrix.1999.mkv", TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.EndsWith("/utilities/guessit", call.Uri.AbsolutePath);
        Assert.Contains("filename=The.Matrix.1999.mkv", call.Uri.Query);
        Assert.Equal("key", call.Headers["Api-Key"]);
        Assert.Equal("MyApp v1", call.Headers["User-Agent"]);
    }

    [Fact]
    public async Task GetUserAsync_Session_SendsBearerToInfosUser()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"data":{"user_id":66,"remaining_downloads":9}}""") };
        var client = Client(handler);
        var user = await client.Info.GetUserAsync(new OpenSubtitlesSession("jwt-1", "vip-api.opensubtitles.com"), TestCt);

        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal("vip-api.opensubtitles.com", call.Uri.Host);
        Assert.EndsWith("/api/v1/infos/user", call.Uri.AbsolutePath);
        Assert.Equal("Bearer jwt-1", call.Headers["Authorization"]);
        Assert.Equal("key", call.Headers["Api-Key"]);
        Assert.Equal(66, user.UserId);
        Assert.Equal(9, user.RemainingDownloads);
    }

    private static OpenSubtitlesClient Client(HttpMessageHandler handler)
        => new(Options(), httpClient: new HttpClient(handler));

    private static OpenSubtitlesClientOptions Options()
        => new() {
            ApiKey = "key",
            ApplicationUserAgent = "MyApp v1",
            BaseUrl = "https://api.opensubtitles.com/api/v1/"
        };

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
