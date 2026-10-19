using Lyo.Common.Metadata.Records;
using Lyo.FileStorage.Web.Components;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileMediaPreviewTests
{
    [Theory]
    [InlineData("photo.png")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.gif")]
    [InlineData("photo.webp")]
    [InlineData("photo.svg")]
    public void PreviewKind_ImageExtensions_IsImage(string name)
        => Assert.Equal(FileMediaPreviewKind.Image, FileMediaPreview.PreviewKind(FileTypeInfo.FromFilePath(name)));

    [Theory]
    [InlineData("song.mp3")]
    [InlineData("song.wav")]
    [InlineData("song.webm")]
    public void PreviewKind_AudioExtensions_IsAudio(string name)
        => Assert.Equal(FileMediaPreviewKind.Audio, FileMediaPreview.PreviewKind(FileTypeInfo.FromFilePath(name)));

    [Fact]
    public void PreviewKind_Mp4_IsVideo()
        => Assert.Equal(FileMediaPreviewKind.Video, FileMediaPreview.PreviewKind(FileTypeInfo.FromFilePath("clip.mp4")));

    [Theory]
    [InlineData("sheet.csv")]
    [InlineData("page.html")]
    public void PreviewKind_Other_ReturnsNone(string name)
        => Assert.Null(FileMediaPreview.PreviewKind(FileTypeInfo.FromFilePath(name)));

    [Fact]
    public void InlineDownloadPath_UsesInlineQuery()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("files/11111111-1111-1111-1111-111111111111/download?inline=true", FileMediaPreview.InlineDownloadPath(id));
    }
}
