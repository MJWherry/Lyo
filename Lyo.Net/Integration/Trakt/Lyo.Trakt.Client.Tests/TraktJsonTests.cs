using System.Text.Json;

namespace Lyo.Trakt.Client.Tests;

public sealed class TraktJsonTests
{
    [Fact]
    public void SearchResult_TypeMovie_ReadsTitleFromMovieWrapper()
    {
        const string json = """
                            {
                              "type": "movie",
                              "score": 1000.5,
                              "movie": { "title": "The Matrix", "ids": { "trakt": 1, "imdb": "tt0133093" } }
                            }
                            """;

        var result = JsonSerializer.Deserialize<TraktSearchResult>(json, TraktJson.Create());
        Assert.NotNull(result);
        Assert.Equal("movie", result.Type);
        Assert.Equal("The Matrix", result.Movie!.Title);
    }

    [Fact]
    public void Show_StatusReturningSeries_DeserializesAsString()
    {
        const string json = """
                            { "title": "Breaking Bad", "status": "returning series", "ids": { "trakt": 1, "imdb": "tt0903747" } }
                            """;

        var show = JsonSerializer.Deserialize<TraktShow>(json, TraktJson.Create());
        Assert.NotNull(show);
        Assert.Equal("returning series", show.Status);
        Assert.Equal("tt0903747", show.Ids!.Imdb);
    }
}
