using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Records;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class MediaContainerTests
{
    [Fact]
    public void TryFromFormat_Mp4_ReturnsBuiltIn()
    {
        Assert.Same(MediaContainer.Mp4, MediaContainer.TryFromFormat("mp4"));
        Assert.Same(MediaContainer.Mp4, MediaContainer.TryFromFormat(".MP4"));
        Assert.True(MediaContainer.Mp4.IsMp4);
    }

    [Fact]
    public void TryFromExtension_S16leAliasesPcm()
        => Assert.Same(MediaContainer.S16le, MediaContainer.TryFromExtension(".pcm"));

    [Fact]
    public void Custom_ReusesRegisteredInstance()
    {
        var first = MediaContainer.Custom("nut");
        var second = MediaContainer.Custom("NUT");
        Assert.Same(first, second);
        Assert.Equal("nut", first.Format);
    }

    [Fact]
    public void TryFromAudioFormat_Wav_Maps()
        => Assert.Same(MediaContainer.Wav, MediaContainer.TryFromAudioFormat(AudioFormat.Wav));

    [Fact]
    public void ToAudioFormat_Mp3_Maps()
        => Assert.Equal(AudioFormat.Mp3, MediaContainer.Mp3.ToAudioFormat());

    [Fact]
    public void TryFromName_Parallel_DoesNotThrow()
        => Parallel.For(0, 64, _ => Assert.Same(MediaContainer.WebM, MediaContainer.TryFromName("WebM")));

    [Fact]
    public void FileTypeInfo_FromExtension_Mp4_IsVideo()
    {
        var t = FileTypeInfo.FromExtension(".mp4");
        Assert.Same(FileTypeInfo.Mp4, t);
        Assert.Equal(FileTypeCategory.Video, t.Category);
        Assert.Equal("video/mp4", t.MimeType);
    }
}
