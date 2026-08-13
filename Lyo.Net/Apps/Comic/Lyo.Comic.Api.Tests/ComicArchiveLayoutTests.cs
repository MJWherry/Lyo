using Lyo.Comic;
using Lyo.Comic.Api.Storage;

namespace Lyo.Comic.Api.Tests;

public sealed class ComicArchiveLayoutTests
{
    [Fact]
    public void TryParseFileId_HttpsRef_ReturnsFalse()
        => Assert.False(ComicArchiveLayout.TryParseFileId("https://picsum.photos/200", out _));

    [Fact]
    public void TryParseFileId_Guid_ReturnsTrue()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.True(ComicArchiveLayout.TryParseFileId(id.ToString(), out var parsed));
        Assert.Equal(id, parsed);
    }

    [Theory]
    [InlineData("/files/{0}")]
    [InlineData("/api/files/{0}")]
    [InlineData("files/{0}")]
    [InlineData("http://localhost:5000/files/{0}")]
    public void TryParseFileId_PathOrUrlWithGuidLeaf_ReturnsTrue(string template)
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.True(ComicArchiveLayout.TryParseFileId(string.Format(template, id), out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void ChapterEntries_ParsesFilesPathImageRef()
    {
        var pageId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var chapter = new ComicChapter();
        var pages = new[] { new ComicPage { PageNumber = 1, ImageRef = $"/api/files/{pageId:D}" } };
        var entries = ComicArchiveLayout.ChapterEntries(chapter, pages);
        Assert.Equal(pageId, Assert.Single(entries).FileId);
    }

    [Fact]
    public void ChapterZipFileName_IncludesSeriesAndNumber()
    {
        var name = ComicArchiveLayout.ChapterZipFileName("One Piece", new ComicChapter { ChapterNumber = 12, Title = "Start" });
        Assert.Equal("One Piece - Ch. 012 - Start.zip", name);
    }

    [Fact]
    public void SeriesZipFileName_UsesSeriesTitle()
        => Assert.Equal("One Piece.zip", ComicArchiveLayout.SeriesZipFileName("One Piece"));

    [Fact]
    public void TryParseRemoteUrl_Picsum_ReturnsTrue()
        => Assert.True(ComicArchiveLayout.TryParseRemoteUrl("https://picsum.photos/640/480", out _));

    [Fact]
    public void ChapterEntries_IncludesHttpsCoverAndGuidPages()
    {
        var pageId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var chapter = new ComicChapter { CoverImageRef = "https://example.com/cover.jpg" };
        var pages = new[] { new ComicPage { PageNumber = 2, ImageRef = pageId.ToString() }, new ComicPage { PageNumber = 1, ImageRef = pageId.ToString() } };
        var entries = ComicArchiveLayout.ChapterEntries(chapter, pages);
        Assert.Equal("cover", entries[0].ZipPath);
        Assert.Equal(new Uri("https://example.com/cover.jpg"), entries[0].RemoteUrl);
        Assert.Equal("001", entries[1].ZipPath);
        Assert.Equal(pageId, entries[1].FileId);
    }

    [Fact]
    public void ChapterEntries_IncludesPicsumPage()
    {
        var chapter = new ComicChapter();
        var pages = new[] { new ComicPage { PageNumber = 1, ImageRef = "https://picsum.photos/200" } };
        var entry = Assert.Single(ComicArchiveLayout.ChapterEntries(chapter, pages));
        Assert.Equal("001", entry.ZipPath);
        Assert.Equal(new Uri("https://picsum.photos/200"), entry.RemoteUrl);
        Assert.Null(entry.FileId);
    }

    [Fact]
    public void ChapterEntries_SkipsEmptyImageRef()
    {
        var chapter = new ComicChapter();
        var pages = new[] { new ComicPage { PageNumber = 1, ImageRef = null }, new ComicPage { PageNumber = 2, ImageRef = "  " } };
        Assert.Empty(ComicArchiveLayout.ChapterEntries(chapter, pages));
    }

    [Fact]
    public void SeriesEntries_NestsVolumeAndUnassignedChapters()
    {
        var seriesId = Guid.NewGuid();
        var volumeId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var series = new ComicSeries { Id = seriesId, Title = "Demo" };
        var volume = new ComicVolume { Id = volumeId, SeriesId = seriesId, VolumeNumber = 1, Title = "Begin" };
        var inVolume = new ComicChapter { Id = Guid.NewGuid(), SeriesId = seriesId, VolumeId = volumeId, ChapterNumber = 1, Title = "First" };
        var loose = new ComicChapter { Id = Guid.NewGuid(), SeriesId = seriesId, ChapterNumber = 10 };
        var pages = new Dictionary<Guid, IReadOnlyList<ComicPage>> {
            [inVolume.Id] = [new ComicPage { ChapterId = inVolume.Id, PageNumber = 1, ImageRef = pageId.ToString() }],
            [loose.Id] = [new ComicPage { ChapterId = loose.Id, PageNumber = 1, ImageRef = pageId.ToString() }]
        };

        var entries = ComicArchiveLayout.SeriesEntries(series, [volume], [inVolume, loose], pages);
        Assert.Contains(entries, e => e.ZipPath == "Demo/Vol. 01 Begin/Ch. 001 First/001");
        Assert.Contains(entries, e => e.ZipPath == "Demo/Ch. 010/001");
    }
}
