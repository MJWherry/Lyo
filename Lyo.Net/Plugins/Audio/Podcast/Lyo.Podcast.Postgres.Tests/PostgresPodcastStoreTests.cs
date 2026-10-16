using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Podcast.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Podcast.Postgres.Tests;

public sealed class PostgresPodcastStoreTests(PodcastPostgresFixture fixture)
{
    private IPodcastStore Store => fixture.ServiceProvider.GetRequiredService<IPodcastStore>();

    [Fact]
    public async Task SaveShow_SeasonsAndEpisodes_OrdersAndRoundTripsGuest()
    {
        var ct = TestCt;
        var show = NewShow($"show-{LyoGuid.CreateV7():N}");
        show.Subtitle = "Weekly";
        show.Seasons = [
            new PodcastSeason {
                SeasonNumber = 2,
                Episodes = [
                    new PodcastEpisode { EpisodeNumber = 2, Title = "S2E2" },
                    new PodcastEpisode {
                        EpisodeNumber = 1,
                        Title = "S2E1",
                        Credits = [new PodcastCredit { DisplayName = "Guest", Role = PodcastCreditRole.Guest, Ordinal = 1 }],
                        Identifiers = [new PodcastIdentifier { Scheme = PodcastIdentifierSchemes.EpisodeGuid, Value = "EP-ABC", IsPrimary = true }]
                    }
                ]
            },
            new PodcastSeason {
                SeasonNumber = 1,
                Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "S1E1" }]
            }
        ];

        await Store.SaveShowAsync(show, ct);
        var loaded = await Store.GetShowByIdAsync(show.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal("Weekly", loaded.Subtitle);
        Assert.Equal([1, 2], loaded.Seasons.Select(s => s.SeasonNumber).ToArray());
        Assert.Equal(["S2E1", "S2E2"], loaded.Seasons[1].Episodes.Select(e => e.Title).ToArray());
        var guestEpisode = loaded.Seasons[1].Episodes[0];
        Assert.Equal(PodcastCreditRole.Guest, Assert.Single(guestEpisode.Credits).Role);
        Assert.Equal("ep-abc", Assert.Single(guestEpisode.Identifiers).NormalizedValue);
    }

    [Fact]
    public async Task SaveShow_DuplicateNumbers_ConflictPerParent()
    {
        var ct = TestCt;
        var duplicateSeason = NewShow($"season-dup-{LyoGuid.CreateV7():N}");
        duplicateSeason.Seasons = [
            new PodcastSeason { SeasonNumber = 1, Episodes = [] },
            new PodcastSeason { SeasonNumber = 1, Episodes = [] }
        ];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveShowAsync(duplicateSeason, ct));

        var twoSeasons = NewShow($"two-seasons-{LyoGuid.CreateV7():N}");
        twoSeasons.Seasons = [
            new PodcastSeason { SeasonNumber = 1, Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "A" }] },
            new PodcastSeason { SeasonNumber = 2, Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "B" }] }
        ];
        await Store.SaveShowAsync(twoSeasons, ct);
        var loaded = await Store.GetShowByIdAsync(twoSeasons.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Seasons.Count);
        Assert.All(loaded.Seasons, s => Assert.Equal(1, Assert.Single(s.Episodes).EpisodeNumber));

        var duplicateEpisode = NewShow($"ep-dup-{LyoGuid.CreateV7():N}");
        duplicateEpisode.Seasons = [
            new PodcastSeason {
                SeasonNumber = 1,
                Episodes = [
                    new PodcastEpisode { EpisodeNumber = 1, Title = "A" },
                    new PodcastEpisode { EpisodeNumber = 1, Title = "B" }
                ]
            }
        ];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveShowAsync(duplicateEpisode, ct));
    }

    [Fact]
    public async Task SaveShow_DuplicateSlugOrItunes_ThrowsConflict_CustomKeepsCase()
    {
        var ct = TestCt;
        var slug = $"slug-{LyoGuid.CreateV7():N}";
        await Store.SaveShowAsync(NewShow(slug), ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveShowAsync(NewShow(slug), ct));

        var first = NewShow($"itunes-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new PodcastIdentifier { Scheme = PodcastIdentifierSchemes.Itunes, Value = "ABC", IsPrimary = true }];
        await Store.SaveShowAsync(first, ct);
        var second = NewShow($"itunes-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new PodcastIdentifier { Scheme = PodcastIdentifierSchemes.Itunes, Value = "abc", IsPrimary = true }];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveShowAsync(second, ct));

        var custom = NewShow($"custom-a-{LyoGuid.CreateV7():N}");
        custom.Identifiers = [new PodcastIdentifier { Scheme = PodcastIdentifierSchemes.Custom, Value = "AbC", IsPrimary = true }];
        await Store.SaveShowAsync(custom, ct);
        Assert.Null(await Store.GetShowByIdentifierAsync(PodcastIdentifierSchemes.Custom, "abc", ct));
        var other = NewShow($"custom-b-{LyoGuid.CreateV7():N}");
        other.Identifiers = [new PodcastIdentifier { Scheme = PodcastIdentifierSchemes.Custom, Value = "abc", IsPrimary = true }];
        await Store.SaveShowAsync(other, ct);
    }

    [Fact]
    public async Task DeleteShow_RemovesSeasonsAndEpisodes()
    {
        var ct = TestCt;
        var show = NewShow($"del-{LyoGuid.CreateV7():N}");
        show.Seasons = [new PodcastSeason { SeasonNumber = 1, Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "Pilot" }] }];
        await Store.SaveShowAsync(show, ct);
        await Store.DeleteShowAsync(show.Id, ct);
        Assert.Null(await Store.GetShowByIdAsync(show.Id, ct));
    }

    [Fact]
    public async Task SaveShow_NullTranscript_KeepsEpisodeText()
    {
        var ct = TestCt;
        var token = $"tr{LyoGuid.CreateV7():N}";
        var other = NewShow($"other-{LyoGuid.CreateV7():N}");
        other.Seasons = [new PodcastSeason { SeasonNumber = 1, Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "Quiet", Transcript = "a quiet unrelated scene" }] }];
        await Store.SaveShowAsync(other, ct);

        var show = NewShow($"keep-{LyoGuid.CreateV7():N}");
        show.Seasons = [new PodcastSeason { SeasonNumber = 1, Episodes = [new PodcastEpisode { EpisodeNumber = 1, Title = "Pilot", Transcript = $"the witness says {token} aloud" }] }];
        await Store.SaveShowAsync(show, ct);

        var loaded = await Store.GetShowByIdAsync(show.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.Seasons[0].Episodes[0].Transcript);
        await Store.SaveShowAsync(loaded, ct);

        var hits = await Store.SearchTranscriptsAsync(new PodcastTranscriptQuery { FullText = token }, ct);
        var hit = Assert.Single(hits);
        Assert.Equal(show.Id, hit.ShowId);
        Assert.Equal(loaded.Seasons[0].Episodes[0].Id, hit.EpisodeId);
    }

    [Fact]
    public async Task GetEpisodeTranscript_MissingId_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => Store.GetEpisodeTranscriptAsync(LyoGuid.CreateV7(), TestCt));

    [Fact]
    public async Task SearchShows_ByStatus_SkipsUnknownAndHiatus_EmptyFilterReturnsNone()
    {
        var ct = TestCt;
        var prefix = $"st-{LyoGuid.CreateV7():N}";
        var ongoing = NewShow($"{prefix}-on");
        ongoing.Title = prefix;
        ongoing.Status = PodcastShowStatus.Ongoing;
        var hiatus = NewShow($"{prefix}-hi");
        hiatus.Title = prefix;
        hiatus.Status = PodcastShowStatus.Hiatus;
        var unknown = NewShow($"{prefix}-un");
        unknown.Title = prefix;
        await Store.SaveShowAsync(ongoing, ct);
        await Store.SaveShowAsync(hiatus, ct);
        await Store.SaveShowAsync(unknown, ct);

        var results = await Store.SearchShowsAsync(new PodcastShowQuery { TitleContains = prefix, Status = PodcastShowStatus.Ongoing }, ct);
        Assert.Equal(ongoing.Id, Assert.Single(results).Id);
        Assert.Empty(await Store.SearchShowsAsync(new PodcastShowQuery { FilterIds = [] }, ct));
    }

    [Fact]
    public async Task SaveGenre_ParentCycle_ThrowsUnprocessable()
    {
        var ct = TestCt;
        var parent = new PodcastGenre { Name = "News", Slug = $"news-{LyoGuid.CreateV7():N}" };
        await Store.SaveGenreAsync(parent, ct);
        var child = new PodcastGenre { Name = "Daily", Slug = $"daily-{LyoGuid.CreateV7():N}", ParentGenreId = parent.Id };
        await Store.SaveGenreAsync(child, ct);
        parent.ParentGenreId = child.Id;
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => Store.SaveGenreAsync(parent, ct));
    }

    private static PodcastShow NewShow(string slug)
        => new() { Title = slug, Slug = slug };
}
