using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegCodecMapTests
{
    [Fact]
    public void ToFfmpegVideo_H264_IsLibx264()
        => Assert.Equal("libx264", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.H264.Id));

    [Fact]
    public void ToFfmpegAudio_Mp3_IsLibmp3lame()
        => Assert.Equal("libmp3lame", FFmpegCodecMap.ToFfmpegAudio(AudioEncoder.Mp3.Id));

    [Fact]
    public void ToFfmpegVideo_Unknown_PassesThrough()
        => Assert.Equal("libx265", FFmpegCodecMap.ToFfmpegVideo("libx265"));

    [Fact]
    public void FromFfmpegAudio_Libmp3lame_IsMp3()
        => Assert.Equal("mp3", FFmpegCodecMap.FromFfmpegAudio("libmp3lame"));

    [Fact]
    public void FromFfmpegVideo_Libx264_IsH264()
        => Assert.Equal("h264", FFmpegCodecMap.FromFfmpegVideo("libx264"));
}
