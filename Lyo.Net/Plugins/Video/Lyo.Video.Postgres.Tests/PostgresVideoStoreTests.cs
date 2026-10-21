using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Video.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Video.Postgres.Tests;

public sealed class PostgresVideoStoreTests(VideoPostgresFixture fixture)
{
    private IVideoStore Store => fixture.ServiceProvider.GetRequiredService<IVideoStore>();

    [Fact]
    public async Task SaveMovie_RoundTrip_CreditsAndImdb()
    {
        var ct = TestCt;
        var title = NewMovie($"movie-{LyoGuid.CreateV7():N}");
        title.Credits = [
            new() { DisplayName = "Director", Role = VideoCreditRole.Director, Ordinal = 1 }, new() {
                DisplayName = "Lead",
                Role = VideoCreditRole.Cast,
                Ordinal = 2,
                CharacterName = "Hero"
            }
        ];

        title.Identifiers = [new() { Scheme = VideoIdentifierSchemes.Imdb, Value = "tt1234567", IsPrimary = true }];
        await Store.SaveTitleAsync(title, ct);
        var loaded = await Store.GetTitleByIdentifierAsync(VideoIdentifierSchemes.Imdb, "TT1234567", ct);
        Assert.NotNull(loaded);
        Assert.Equal(title.Id, loaded.Id);
        Assert.Equal("Hero", loaded.Credits.Single(c => c.Role == VideoCreditRole.Cast).CharacterName);
        Assert.Equal("tt1234567", loaded.Identifiers[0].NormalizedValue);
    }

    [Fact]
    public async Task SaveMovie_WithSeason_ThrowsUnprocessable()
    {
        var ct = TestCt;
        var title = NewMovie($"noseason-{LyoGuid.CreateV7():N}");
        title.Seasons = [new() { SeasonNumber = 1, Episodes = [] }];
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => Store.SaveTitleAsync(title, ct));
    }

    [Fact]
    public async Task SaveSeries_SeasonsAndEpisodes_OrdersByNumber()
    {
        var ct = TestCt;
        var title = NewSeries($"series-{LyoGuid.CreateV7():N}");
        title.Seasons = [
            new() { SeasonNumber = 2, Episodes = [new() { EpisodeNumber = 2, Title = "S2E2" }, new() { EpisodeNumber = 1, Title = "S2E1" }] },
            new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "S1E1" }] }
        ];

        await Store.SaveTitleAsync(title, ct);
        var loaded = await Store.GetTitleByIdAsync(title.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal([1, 2], loaded.Seasons.Select(s => s.SeasonNumber).ToArray());
        Assert.Equal(["S2E1", "S2E2"], loaded.Seasons[1].Episodes.Select(e => e.Title).ToArray());
    }

    [Fact]
    public async Task SaveTitle_DuplicateSlugOrImdb_ThrowsConflict()
    {
        var ct = TestCt;
        var slug = $"slug-{LyoGuid.CreateV7():N}";
        await Store.SaveTitleAsync(NewMovie(slug), ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveTitleAsync(NewMovie(slug), ct));
        var first = NewMovie($"imdb-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new() { Scheme = VideoIdentifierSchemes.Imdb, Value = "tt9990001", IsPrimary = true }];
        await Store.SaveTitleAsync(first, ct);
        var second = NewMovie($"imdb-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new() { Scheme = VideoIdentifierSchemes.Imdb, Value = "TT9990001", IsPrimary = true }];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveTitleAsync(second, ct));
    }

    [Fact]
    public async Task DeleteSeries_RemovesSeasonsAndEpisodes()
    {
        var ct = TestCt;
        var title = NewSeries($"del-{LyoGuid.CreateV7():N}");
        title.Seasons = [new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "Pilot" }] }];
        await Store.SaveTitleAsync(title, ct);
        await Store.DeleteTitleAsync(title.Id, ct);
        Assert.Null(await Store.GetTitleByIdAsync(title.Id, ct));
    }

    [Fact]
    public async Task SaveMovie_YoutubeLink_RoundTrips()
    {
        var ct = TestCt;
        var title = NewMovie($"yt-{LyoGuid.CreateV7():N}");
        title.Youtube = new() { VideoId = "dQw4w9WgXcQ", ChannelId = "UCxxxxxxxxxxxxxxxxxxxxxx", PlaylistId = "PLyyyyyyyyyyyyyyyyyyy" };
        await Store.SaveTitleAsync(title, ct);
        var loaded = await Store.GetTitleByIdAsync(title.Id, ct);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Youtube);
        Assert.Equal("dQw4w9WgXcQ", loaded.Youtube.VideoId);
        Assert.Equal("UCxxxxxxxxxxxxxxxxxxxxxx", loaded.Youtube.ChannelId);
        Assert.Equal("PLyyyyyyyyyyyyyyyyyyy", loaded.Youtube.PlaylistId);
    }

    [Fact]
    public async Task SaveEpisode_DuplicateYoutubeVideoId_ThrowsConflict()
    {
        var ct = TestCt;
        var videoId = $"v{LyoGuid.CreateV7():N}"[..11];
        var first = NewSeries($"yt-a-{LyoGuid.CreateV7():N}");
        first.Seasons = [
            new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "A", Youtube = new() { VideoId = videoId, ChannelId = "UCaaaaaaaaaaaaaaaaaaaaaa" } }] }
        ];

        await Store.SaveTitleAsync(first, ct);
        var second = NewSeries($"yt-b-{LyoGuid.CreateV7():N}");
        second.Seasons = [
            new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "B", Youtube = new() { VideoId = videoId, ChannelId = "UCbbbbbbbbbbbbbbbbbbbbbb" } }] }
        ];

        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveTitleAsync(second, ct));
    }

    [Fact]
    public async Task SearchTitles_ByKindAndYear_FiltersMovies()
    {
        var ct = TestCt;
        var prefix = $"yr-{LyoGuid.CreateV7():N}";
        var movie = NewMovie($"{prefix}-movie");
        movie.Year = 1999;
        var series = NewSeries($"{prefix}-series");
        series.Year = 1999;
        await Store.SaveTitleAsync(movie, ct);
        await Store.SaveTitleAsync(series, ct);
        var results = await Store.SearchTitlesAsync(new() { TitleContains = prefix, Kind = VideoTitleKind.Movie, Year = 1999 }, ct);
        Assert.Equal(movie.Id, Assert.Single(results).Id);
    }

    [Fact]
    public async Task DeleteCollection_ClearsTitles()
    {
        var ct = TestCt;
        var collection = new VideoCollection { Title = "Franchise", Slug = $"col-{LyoGuid.CreateV7():N}" };
        await Store.SaveCollectionAsync(collection, ct);
        var title = NewMovie($"colmem-{LyoGuid.CreateV7():N}");
        title.CollectionId = collection.Id;
        await Store.SaveTitleAsync(title, ct);
        await Store.DeleteCollectionAsync(collection.Id, ct);
        var loaded = await Store.GetTitleByIdAsync(title.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.CollectionId);
    }

    [Fact]
    public async Task SaveCut_SecondPrimary_ThrowsConflict()
    {
        var ct = TestCt;
        var title = NewMovie($"cut-{LyoGuid.CreateV7():N}");
        await Store.SaveTitleAsync(title, ct);
        await Store.SaveCutAsync(new() { TitleId = title.Id, Name = "Theatrical", IsPrimary = true }, ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveCutAsync(new() { TitleId = title.Id, Name = "Director", IsPrimary = true }, ct));
    }

    [Fact]
    public async Task SearchTranscripts_MatchesEpisode_NotUnrelatedMovie()
    {
        var ct = TestCt;
        var token = $"tr{LyoGuid.CreateV7():N}";
        var movie = NewMovie($"trmovie-{LyoGuid.CreateV7():N}");
        movie.Transcript = "a quiet unrelated scene";
        await Store.SaveTitleAsync(movie, ct);
        var series = NewSeries($"trseries-{LyoGuid.CreateV7():N}");
        series.Seasons = [new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "Pilot", Transcript = $"the witness says {token} aloud" }] }];
        await Store.SaveTitleAsync(series, ct);
        var hits = await Store.SearchTranscriptsAsync(new() { FullText = token }, ct);
        var hit = Assert.Single(hits);
        Assert.Equal(series.Id, hit.TitleId);
        Assert.Equal(series.Seasons[0].Episodes[0].Id, hit.EpisodeId);
        Assert.DoesNotContain(hits, h => h.TitleId == movie.Id);
        var loaded = await Store.GetTitleByIdAsync(series.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.Seasons[0].Episodes[0].Transcript);
    }

    private static VideoTitle NewMovie(string slug)
        => new() {
            Title = slug,
            Slug = slug,
            Kind = VideoTitleKind.Movie,
            Year = 2020,
            RuntimeSeconds = 100 * 60
        };

    private static VideoTitle NewSeries(string slug)
        => new() {
            Title = slug,
            Slug = slug,
            Kind = VideoTitleKind.Series,
            Status = VideoSeriesStatus.Ongoing
        };
}