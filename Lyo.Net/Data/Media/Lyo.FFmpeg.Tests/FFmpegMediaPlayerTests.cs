using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegMediaPlayerTests
{
    [Fact]
    public void BuildPlayArguments_AudioDefaults_NodispAutoexitAndQuiet()
    {
        var args = FFmpegAudioPlayer.BuildPlayArguments("clip.mp3", new AudioPlayOptions(), suppressOutput: true);
        Assert.Contains("-nodisp", args);
        Assert.Contains("-autoexit", args);
        Assert.Contains("-loglevel", args);
        Assert.Contains("quiet", args);
        var i = args.IndexOf("-i");
        Assert.Equal("clip.mp3", args[i + 1]);
    }

    [Fact]
    public void BuildPlayArguments_VideoWindowed_OmitsNodisp()
    {
        var args = FFmpegVideoPlayer.BuildPlayArguments("clip.mp4", new VideoPlayOptions { NoDisplay = false }, suppressOutput: false);
        Assert.DoesNotContain("-nodisp", args);
        Assert.DoesNotContain("-loglevel", args);
    }

    [Fact]
    public void BuildPlayArguments_Trim_SsBeforeInputAndTAfter()
    {
        var args = FFmpegVideoPlayer.BuildPlayArguments(
            "clip.mp4",
            new VideoPlayOptions { StartTime = TimeSpan.FromSeconds(1), Duration = TimeSpan.FromSeconds(2) },
            suppressOutput: false);
        var ss = args.IndexOf("-ss");
        var i = args.IndexOf("-i");
        var t = args.IndexOf("-t");
        Assert.True(ss < i);
        Assert.True(t > i);
        Assert.Equal("1", args[ss + 1]);
        Assert.Equal("2", args[t + 1]);
    }
}
