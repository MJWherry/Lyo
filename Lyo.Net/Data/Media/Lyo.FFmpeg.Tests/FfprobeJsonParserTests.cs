namespace Lyo.FFmpeg.Tests;

public class FfprobeJsonParserTests
{
    [Fact]
    public void Parse_AudioOnly_StringSampleRate()
    {
        const string json = """
            {
              "format": { "duration": "1.5", "format_name": "mp3", "bit_rate": "128000", "size": "24000",
                "tags": { "title": "Clip", "artist": "Test" } },
              "streams": [
                { "index": 0, "codec_type": "audio", "codec_name": "mp3", "sample_rate": "44100", "channels": 2 }
              ]
            }
            """;
        var result = FfprobeJsonParser.Parse("/tmp/a.mp3", json);
        Assert.True(result.IsSuccess);
        var probe = result.Data!;
        Assert.Equal(1.5, probe.DurationSeconds);
        Assert.Equal(44100, probe.SampleRate);
        Assert.Equal("mp3", probe.Codec);
        Assert.True(probe.HasAudio);
        Assert.False(probe.HasVideo);
        Assert.Equal("Clip", probe.RawMetadata!["title"]);
        Assert.Equal("/tmp/a.mp3", probe.FilePath);
    }

    [Fact]
    public void Parse_VideoAndAudio_FpsRational()
    {
        const string json = """
            {
              "format": { "duration": "2.0", "format_name": "mov,mp4,m4a,3gp,3g2,mj2" },
              "streams": [
                { "index": 0, "codec_type": "video", "codec_name": "h264", "width": 1920, "height": 1080,
                  "pix_fmt": "yuv420p", "avg_frame_rate": "30/1", "r_frame_rate": "30/1",
                  "disposition": { "attached_pic": 0 } },
                { "index": 1, "codec_type": "audio", "codec_name": "aac", "sample_rate": "48000", "channels": 2 }
              ]
            }
            """;
        var probe = FfprobeJsonParser.Parse("clip.mp4", json).Data!;
        Assert.True(probe.HasVideo);
        Assert.True(probe.HasAudio);
        Assert.Equal("h264", probe.VideoCodec);
        Assert.Equal(1920, probe.Width);
        Assert.Equal(1080, probe.Height);
        Assert.Equal(30, probe.Fps);
        Assert.Equal(2, probe.Streams.Count);
    }

    [Fact]
    public void Parse_MultiStream_KeepsAllStreams()
    {
        const string json = """
            {
              "format": { "duration": "3.0", "format_name": "matroska,webm" },
              "streams": [
                { "index": 0, "codec_type": "video", "codec_name": "vp9", "width": 640, "height": 360, "avg_frame_rate": "24/1" },
                { "index": 1, "codec_type": "audio", "codec_name": "opus", "sample_rate": "48000", "channels": 2 },
                { "index": 2, "codec_type": "audio", "codec_name": "opus", "sample_rate": "48000", "channels": 1 }
              ]
            }
            """;
        var probe = FfprobeJsonParser.Parse("clip.webm", json).Data!;
        Assert.Equal(3, probe.Streams.Count);
        Assert.Equal("vp9", probe.VideoCodec);
        Assert.Equal("opus", probe.Codec);
        Assert.Equal(2, probe.Channels);
    }

    [Fact]
    public void Parse_AttachedPic_IsNotHasVideo()
    {
        const string json = """
            {
              "format": { "format_name": "mp3" },
              "streams": [
                { "index": 0, "codec_type": "audio", "codec_name": "mp3", "sample_rate": "44100", "channels": 2 },
                { "index": 1, "codec_type": "video", "codec_name": "mjpeg", "width": 600, "height": 600,
                  "disposition": { "attached_pic": 1 } }
              ]
            }
            """;
        var probe = FfprobeJsonParser.Parse("song.mp3", json).Data!;
        Assert.True(probe.HasAudio);
        Assert.False(probe.HasVideo);
        Assert.Null(probe.VideoCodec);
        Assert.True(probe.Streams[1].IsAttachedPicture);
    }

    [Fact]
    public void TryParseRational_RejectsZeroOverZero()
        => Assert.False(FfprobeJsonParser.TryParseRational("0/0", out _));

    [Fact]
    public void Parse_StreamSource_FilePathNull()
    {
        const string json = """{ "format": { "duration": "1" }, "streams": [] }""";
        var probe = FfprobeJsonParser.Parse(null, json).Data!;
        Assert.Null(probe.FilePath);
    }
}
