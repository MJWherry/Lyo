using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegCodecMapTests
{
    [Fact]
    public void ToFfmpegVideo_H264_IsLibx264() => Assert.Equal("libx264", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.H264.Id));

    [Fact]
    public void ToFfmpegAudio_Mp3_IsLibmp3lame() => Assert.Equal("libmp3lame", FFmpegCodecMap.ToFfmpegAudio(AudioEncoder.Mp3.Id));

    [Fact]
    public void ToFfmpegVideo_Unknown_PassesThrough() => Assert.Equal("libx265", FFmpegCodecMap.ToFfmpegVideo("libx265"));

    [Fact]
    public void FromFfmpegAudio_Libmp3lame_IsMp3() => Assert.Equal("mp3", FFmpegCodecMap.FromFfmpegAudio("libmp3lame"));

    [Fact]
    public void FromFfmpegVideo_Libx264_IsH264() => Assert.Equal("h264", FFmpegCodecMap.FromFfmpegVideo("libx264"));

    [Fact]
    public void ToFfmpegVideo_HevcAv1Vp8_MapsSoftwareEncoders()
    {
        Assert.Equal("libx265", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.Hevc.Id));
        Assert.Equal("libsvtav1", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.Av1.Id));
        Assert.Equal("libvpx", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.Vp8.Id));
    }

    [Fact]
    public void ToFfmpegAudio_VorbisAndFlac()
    {
        Assert.Equal("libvorbis", FFmpegCodecMap.ToFfmpegAudio(AudioEncoder.Vorbis.Id));
        Assert.Equal("flac", FFmpegCodecMap.ToFfmpegAudio(AudioEncoder.Flac.Id));
    }

    [Fact]
    public void ToFfmpegVideo_Hardware_SwapsEncoderName()
    {
        Assert.Equal("h264_nvenc", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.H264.Id, HardwareAccel.Nvenc));
        Assert.Equal("hevc_qsv", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.Hevc.Id, HardwareAccel.Qsv));
        Assert.Equal("h264_videotoolbox", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.H264.Id, HardwareAccel.VideoToolbox));
        Assert.Equal("h264_vaapi", FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.H264.Id, HardwareAccel.Vaapi));
        Assert.Throws<ArgumentException>(() => FFmpegCodecMap.ToFfmpegVideo(VideoEncoder.Mpeg4.Id, HardwareAccel.Nvenc));
    }
}