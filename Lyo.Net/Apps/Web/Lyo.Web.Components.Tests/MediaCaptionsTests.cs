using Lyo.Web.Components.Media;

namespace Lyo.Web.Components.Tests;

public sealed class MediaCaptionsTests
{
    [Fact]
    public void Resolve_BlankSrc_DropsTrack()
    {
        var tracks = MediaCaptions.Resolve([
            new MediaCaptionTrack("  "),
            new MediaCaptionTrack("  subs.vtt ", Label: "English", Language: "en")
        ]);

        var track = Assert.Single(tracks);
        Assert.Equal("subs.vtt", track.Src);
        Assert.Equal("English", track.Label);
        Assert.Equal("en", track.Language);
    }

    [Fact]
    public void Resolve_Null_ReturnsEmpty() => Assert.Empty(MediaCaptions.Resolve(null));

    [Fact]
    public void Resolve_TwoDefaults_KeepsFirst()
    {
        var tracks = MediaCaptions.Resolve([
            new MediaCaptionTrack("a.vtt", Default: true),
            new MediaCaptionTrack("b.vtt", Default: true),
            new MediaCaptionTrack("c.vtt")
        ]);

        Assert.Equal(3, tracks.Count);
        Assert.True(tracks[0].Default);
        Assert.False(tracks[1].Default);
        Assert.False(tracks[2].Default);
    }

    [Theory]
    [InlineData(MediaCaptionKind.Subtitles, "subtitles")]
    [InlineData(MediaCaptionKind.Captions, "captions")]
    [InlineData(MediaCaptionKind.Descriptions, "descriptions")]
    [InlineData(MediaCaptionKind.Chapters, "chapters")]
    [InlineData(MediaCaptionKind.Metadata, "metadata")]
    public void HtmlKind_MapsEveryKind(MediaCaptionKind kind, string html) => Assert.Equal(html, MediaCaptions.HtmlKind(kind));
}
