using Lyo.Web.Components.Media;

namespace Lyo.Web.Components.Tests;

public sealed class MediaSourceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Normalize_Whitespace_ReturnsNull(string? src) => Assert.Null(MediaSource.Normalize(src));

    [Fact]
    public void Normalize_PaddedSrc_Trims() => Assert.Equal("clip.mp4", MediaSource.Normalize(" clip.mp4 "));

    [Fact]
    public void ImageSrc_BlankSrc_UsesPoster()
    {
        Assert.Equal("cover.png", MediaSource.ImageSrc(null, "cover.png"));
        Assert.Equal("cover.png", MediaSource.ImageSrc("  ", "  cover.png "));
        Assert.Equal("photo.png", MediaSource.ImageSrc(" photo.png ", "cover.png"));
        Assert.Null(MediaSource.ImageSrc(null, "  "));
    }
}