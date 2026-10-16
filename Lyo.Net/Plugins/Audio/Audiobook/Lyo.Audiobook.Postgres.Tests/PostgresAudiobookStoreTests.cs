using Lyo.Audiobook.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Audiobook.Postgres.Tests;

public sealed class PostgresAudiobookStoreTests(AudiobookPostgresFixture fixture)
{
    private IAudiobookStore Store => fixture.ServiceProvider.GetRequiredService<IAudiobookStore>();

    [Fact]
    public void Normalize_Isbn_KeepsDigitsAndTrailingX()
    {
        Assert.Equal("9780123", AudiobookIdentifierSchemes.Normalize(AudiobookIdentifierSchemes.Isbn, "978-0-123"));
        Assert.Equal("080442957X", AudiobookIdentifierSchemes.Normalize(AudiobookIdentifierSchemes.Isbn, "0-8044-2957-x"));
    }

    [Fact]
    public void Normalize_Custom_PreservesCase()
        => Assert.Equal("AbC", AudiobookIdentifierSchemes.Normalize(AudiobookIdentifierSchemes.Custom, " AbC "));

    [Fact]
    public async Task SaveWork_RoundTrip_SubtitleNarratorAndIsbn()
    {
        var ct = TestCt;
        var work = NewWork($"book-{LyoGuid.CreateV7():N}");
        work.Subtitle = "A reading";
        work.Credits = [new AudiobookCredit { DisplayName = "Narrator", Role = AudiobookCreditRole.Narrator, Ordinal = 1, PersonRef = "person:1" }];
        work.Identifiers = [new AudiobookIdentifier { Scheme = AudiobookIdentifierSchemes.Isbn, Value = "978-0-306-40615-7", IsPrimary = true }];

        await Store.SaveWorkAsync(work, ct);
        var loaded = await Store.GetWorkByIdentifierAsync(AudiobookIdentifierSchemes.Isbn, "978-0-306-40615-7", ct);
        Assert.NotNull(loaded);
        Assert.Equal(work.Id, loaded.Id);
        Assert.Equal("A reading", loaded.Subtitle);
        Assert.Equal("Narrator", loaded.Credits.Single(c => c.Role == AudiobookCreditRole.Narrator).DisplayName);
        Assert.Equal("9780306406157", loaded.Identifiers[0].NormalizedValue);
    }

    [Fact]
    public async Task SaveWork_DuplicateSlugOrIsbn_ThrowsConflict()
    {
        var ct = TestCt;
        var slug = $"slug-{LyoGuid.CreateV7():N}";
        await Store.SaveWorkAsync(NewWork(slug), ct);
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveWorkAsync(NewWork(slug), ct));

        var first = NewWork($"isbn-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new AudiobookIdentifier { Scheme = AudiobookIdentifierSchemes.Isbn, Value = "978-0-123", IsPrimary = true }];
        await Store.SaveWorkAsync(first, ct);
        var second = NewWork($"isbn-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new AudiobookIdentifier { Scheme = AudiobookIdentifierSchemes.Isbn, Value = "9780123", IsPrimary = true }];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveWorkAsync(second, ct));
    }

    [Fact]
    public async Task SaveWork_CustomDifferentCase_DoesNotMatch()
    {
        var ct = TestCt;
        var first = NewWork($"custom-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new AudiobookIdentifier { Scheme = AudiobookIdentifierSchemes.Custom, Value = "AbC", IsPrimary = true }];
        await Store.SaveWorkAsync(first, ct);

        Assert.Null(await Store.GetWorkByIdentifierAsync(AudiobookIdentifierSchemes.Custom, "abc", ct));

        var second = NewWork($"custom-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new AudiobookIdentifier { Scheme = AudiobookIdentifierSchemes.Custom, Value = "abc", IsPrimary = true }];
        await Store.SaveWorkAsync(second, ct);
        var loaded = await Store.GetWorkByIdentifierAsync(AudiobookIdentifierSchemes.Custom, "abc", ct);
        Assert.NotNull(loaded);
        Assert.Equal(second.Id, loaded.Id);
    }

    [Fact]
    public async Task SaveWork_Chapters_OrdersAndConflictsPerWork()
    {
        var ct = TestCt;
        var work = NewWork($"ch-{LyoGuid.CreateV7():N}");
        work.Chapters = [
            new AudiobookChapter { Number = 2, Title = "Two" },
            new AudiobookChapter { Number = 1, Title = "One" }
        ];
        await Store.SaveWorkAsync(work, ct);
        var loaded = await Store.GetWorkByIdAsync(work.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal(["One", "Two"], loaded.Chapters.Select(c => c.Title).ToArray());

        var duplicate = NewWork($"ch-dup-{LyoGuid.CreateV7():N}");
        duplicate.Chapters = [
            new AudiobookChapter { Number = 1, Title = "A" },
            new AudiobookChapter { Number = 1, Title = "B" }
        ];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveWorkAsync(duplicate, ct));

        var other = NewWork($"ch-other-{LyoGuid.CreateV7():N}");
        other.Chapters = [new AudiobookChapter { Number = 1, Title = "Also one" }];
        await Store.SaveWorkAsync(other, ct);
        var otherLoaded = await Store.GetWorkByIdAsync(other.Id, ct);
        Assert.NotNull(otherLoaded);
        Assert.Equal(1, Assert.Single(otherLoaded.Chapters).Number);
    }

    [Fact]
    public async Task DeleteSeries_ClearsSeriesIdAndIndex()
    {
        var ct = TestCt;
        var series = new AudiobookSeries { Title = "Cycle", Slug = $"ser-{LyoGuid.CreateV7():N}" };
        await Store.SaveSeriesAsync(series, ct);
        var work = NewWork($"member-{LyoGuid.CreateV7():N}");
        work.SeriesId = series.Id;
        work.SeriesIndex = 1.5m;
        await Store.SaveWorkAsync(work, ct);

        await Store.DeleteSeriesAsync(series.Id, ct);

        var loaded = await Store.GetWorkByIdAsync(work.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.SeriesId);
        Assert.Null(loaded.SeriesIndex);
        Assert.Null(await Store.GetSeriesByIdAsync(series.Id, ct));
    }

    [Fact]
    public async Task SaveWork_NullTranscript_KeepsChapterAndWorkText()
    {
        var ct = TestCt;
        var token = $"tr{LyoGuid.CreateV7():N}";
        var other = NewWork($"other-{LyoGuid.CreateV7():N}");
        other.Transcript = "a quiet unrelated scene";
        await Store.SaveWorkAsync(other, ct);

        var work = NewWork($"keep-{LyoGuid.CreateV7():N}");
        work.Transcript = $"work says {token} once";
        work.Chapters = [new AudiobookChapter { Number = 1, Title = "Open", Transcript = $"chapter says {token} aloud" }];
        await Store.SaveWorkAsync(work, ct);

        var loaded = await Store.GetWorkByIdAsync(work.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.Transcript);
        Assert.Null(loaded.Chapters[0].Transcript);

        await Store.SaveWorkAsync(loaded, ct);

        var hits = await Store.SearchTranscriptsAsync(new AudiobookTranscriptQuery { FullText = token }, ct);
        Assert.Contains(hits, h => h.WorkId == work.Id && h.ChapterId == null);
        Assert.Contains(hits, h => h.WorkId == work.Id && h.ChapterId == loaded.Chapters[0].Id);
        Assert.DoesNotContain(hits, h => h.WorkId == other.Id);
    }

    [Fact]
    public async Task GetChapterTranscript_MissingId_ThrowsNotFound()
    {
        var ct = TestCt;
        await Assert.ThrowsAsync<NotFoundException>(() => Store.GetChapterTranscriptAsync(LyoGuid.CreateV7(), ct));
        await Assert.ThrowsAsync<NotFoundException>(() => Store.GetWorkTranscriptAsync(LyoGuid.CreateV7(), ct));
    }

    [Fact]
    public async Task SearchWorks_ByYear_AndEmptyFilterIdsReturnsNone()
    {
        var ct = TestCt;
        var prefix = $"yr-{LyoGuid.CreateV7():N}";
        var match = NewWork($"{prefix}-match");
        match.Year = 1999;
        match.Title = prefix;
        var other = NewWork($"{prefix}-other");
        other.Year = 2001;
        other.Title = prefix;
        await Store.SaveWorkAsync(match, ct);
        await Store.SaveWorkAsync(other, ct);

        var results = await Store.SearchWorksAsync(new AudiobookQuery { TitleContains = prefix, Year = 1999 }, ct);
        Assert.Equal(match.Id, Assert.Single(results).Id);
        Assert.Empty(await Store.SearchWorksAsync(new AudiobookQuery { FilterIds = [] }, ct));
    }

    [Fact]
    public async Task SaveGenre_ParentCycle_ThrowsUnprocessable()
    {
        var ct = TestCt;
        var parent = new AudiobookGenre { Name = "Fiction", Slug = $"fic-{LyoGuid.CreateV7():N}" };
        await Store.SaveGenreAsync(parent, ct);
        var child = new AudiobookGenre { Name = "Mystery", Slug = $"mys-{LyoGuid.CreateV7():N}", ParentGenreId = parent.Id };
        await Store.SaveGenreAsync(child, ct);
        parent.ParentGenreId = child.Id;
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => Store.SaveGenreAsync(parent, ct));
    }

    private static Audiobook NewWork(string slug)
        => new() { Title = slug, Slug = slug };
}
