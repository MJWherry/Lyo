using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Music.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Music.Postgres.Tests;

public sealed class PostgresMusicStoreTests(MusicPostgresFixture fixture)
{
    private IMusicStore Store => fixture.ServiceProvider.GetRequiredService<IMusicStore>();

    [Fact]
    public async Task SaveRelease_Contributors_SetsPrimaryArtist()
    {
        var ct = TestCt;
        var release = NewRelease($"artist-{LyoGuid.CreateV7():N}");
        release.Credits = [
            new MusicCredit { DisplayName = "Second", Role = MusicCreditRole.Artist, Ordinal = 2 },
            new MusicCredit { DisplayName = "First", Role = MusicCreditRole.Featured, Ordinal = 1 },
            new MusicCredit { DisplayName = "Producer", Role = MusicCreditRole.Producer, Ordinal = 0 }
        ];

        await Store.SaveReleaseAsync(release, ct);
        var loaded = await Store.GetReleaseByIdAsync(release.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal("First", loaded.PrimaryArtist);
    }

    [Fact]
    public async Task SaveRelease_DuplicateSlugOrBarcode_ThrowsConflict()
    {
        var ct = TestCt;
        var slug = $"slug-{LyoGuid.CreateV7():N}";
        await Store.SaveReleaseAsync(NewRelease(slug), ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveReleaseAsync(NewRelease(slug), ct));

        var barcode = "123456789012";
        var first = NewRelease($"bar-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new MusicIdentifier { Scheme = MusicIdentifierSchemes.Barcode, Value = barcode, IsPrimary = true }];
        await Store.SaveReleaseAsync(first, ct);

        var second = NewRelease($"bar-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new MusicIdentifier { Scheme = MusicIdentifierSchemes.Barcode, Value = "1234-5678-9012", IsPrimary = true }];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveReleaseAsync(second, ct));
    }

    [Fact]
    public async Task GetReleaseByIdentifier_Barcode_NormalizesSeparators()
    {
        var ct = TestCt;
        var release = NewRelease($"norm-{LyoGuid.CreateV7():N}");
        release.Identifiers = [new MusicIdentifier { Scheme = MusicIdentifierSchemes.Barcode, Value = "12345678901", IsPrimary = true }];
        await Store.SaveReleaseAsync(release, ct);

        var found = await Store.GetReleaseByIdentifierAsync(MusicIdentifierSchemes.Barcode, "1 2345-67890-1", ct);
        Assert.NotNull(found);
        Assert.Equal(release.Id, found.Id);
        Assert.Equal("12345678901", found.Identifiers[0].NormalizedValue);
    }

    [Fact]
    public async Task DeleteReleaseGroup_ClearsGroupOnReleases()
    {
        var ct = TestCt;
        var group = new MusicReleaseGroup { Title = "Group", Slug = $"grp-{LyoGuid.CreateV7():N}", Kind = MusicReleaseKind.Album };
        await Store.SaveReleaseGroupAsync(group, ct);
        var release = NewRelease($"mem-{LyoGuid.CreateV7():N}");
        release.ReleaseGroupId = group.Id;
        await Store.SaveReleaseAsync(release, ct);

        await Store.DeleteReleaseGroupAsync(group.Id, ct);

        var loaded = await Store.GetReleaseByIdAsync(release.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.ReleaseGroupId);
        Assert.Null(await Store.GetReleaseGroupByIdAsync(group.Id, ct));
    }

    [Fact]
    public async Task SaveRelease_DuplicateTrackPosition_ThrowsConflict()
    {
        var ct = TestCt;
        var release = NewRelease($"trk-{LyoGuid.CreateV7():N}");
        release.Tracks = [
            new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "A" },
            new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "B" }
        ];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveReleaseAsync(release, ct));
    }

    [Fact]
    public async Task DeleteRelease_RemovesTracks()
    {
        var ct = TestCt;
        var release = NewRelease($"del-{LyoGuid.CreateV7():N}");
        release.Tracks = [new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "Only" }];
        await Store.SaveReleaseAsync(release, ct);

        await Store.DeleteReleaseAsync(release.Id, ct);

        Assert.Null(await Store.GetReleaseByIdAsync(release.Id, ct));
    }

    [Fact]
    public async Task SearchReleases_EmptyFilterIds_ReturnsNone()
    {
        var ct = TestCt;
        var slug = $"flt-{LyoGuid.CreateV7():N}";
        await Store.SaveReleaseAsync(NewRelease(slug), ct);
        var results = await Store.SearchReleasesAsync(new MusicReleaseQuery { TitleContains = slug, FilterIds = [] }, ct);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SaveRecording_OnTwoReleases_GetRecordingReturnsBothTracks()
    {
        var ct = TestCt;
        var recording = new MusicRecording { Title = "Shared" };
        await Store.SaveRecordingAsync(recording, ct);
        var first = NewRelease($"rec-a-{LyoGuid.CreateV7():N}");
        first.Tracks = [new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "On A", RecordingId = recording.Id }];
        var second = NewRelease($"rec-b-{LyoGuid.CreateV7():N}");
        second.Tracks = [new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "On B", RecordingId = recording.Id }];
        await Store.SaveReleaseAsync(first, ct);
        await Store.SaveReleaseAsync(second, ct);

        var loaded = await Store.GetRecordingAsync(recording.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Tracks.Count);
        Assert.Contains(loaded.Tracks, t => t.ReleaseId == first.Id);
        Assert.Contains(loaded.Tracks, t => t.ReleaseId == second.Id);
    }

    [Fact]
    public async Task SaveGenre_SelfParent_ThrowsUnprocessable()
    {
        var ct = TestCt;
        var id = LyoGuid.CreateV7();
        var genre = new MusicGenre { Id = id, ParentGenreId = id, Slug = $"self-{id:N}", Name = "Self" };
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => Store.SaveGenreAsync(genre, ct));
    }

    [Fact]
    public async Task DeleteGenre_WhenReferenced_ThrowsConflict()
    {
        var ct = TestCt;
        var genre = new MusicGenre { Slug = $"used-{LyoGuid.CreateV7():N}", Name = "Used" };
        await Store.SaveGenreAsync(genre, ct);
        var release = NewRelease($"gen-{LyoGuid.CreateV7():N}");
        release.GenreIds = [genre.Id];
        await Store.SaveReleaseAsync(release, ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.DeleteGenreAsync(genre.Id, ct));
    }

    [Fact]
    public async Task SearchLyrics_FindsToken_OmitsLyricsFromReleaseList()
    {
        var ct = TestCt;
        var token = $"lyric{LyoGuid.CreateV7():N}";
        var release = NewRelease($"lyr-{LyoGuid.CreateV7():N}");
        release.Tracks = [new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "Verse", Lyrics = $"a chorus about {token} tonight" }];
        await Store.SaveReleaseAsync(release, ct);

        var hits = await Store.SearchLyricsAsync(new MusicLyricsQuery { FullText = token }, ct);
        var hit = Assert.Single(hits);
        Assert.Equal(release.Id, hit.ReleaseId);
        Assert.Contains(token, hit.Snippet, StringComparison.OrdinalIgnoreCase);

        var loaded = await Store.GetReleaseByIdAsync(release.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.Tracks[0].Lyrics);
    }

    private static MusicRelease NewRelease(string slug)
        => new() {
            Title = slug,
            Slug = slug,
            Kind = MusicReleaseKind.Album,
            Status = MusicReleaseStatus.Released
        };
}
