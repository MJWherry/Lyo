using CliWrap;
using Lyo.IO.Temp.Models;

namespace Lyo.FileTags.FFmpeg.Tests;

public class FFmpegFileTagIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Write_StreamCopy_KeepsWavCodec()
    {
        Assert.SkipUnless(await HasToolAsync("ffmpeg") && await HasToolAsync("ffprobe"), "ffmpeg/ffprobe are not on PATH.");
        using var session = new IOTempSession();
        var wav = session.GetFilePath("tone.wav");
        await Cli.Wrap("ffmpeg")
            .WithArguments("-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=0.2", "-c:a", "pcm_s16le", wav)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(TestCt);

        var before = await CodecAsync(wav);
        var provider = new FFmpegFileTagProvider(new());
        var written = await provider.WriteAsync(wav, new(new Dictionary<FileTagKey, string> { [FileTagKey.Title] = "Show" }), TestCt);
        Assert.True(written.IsSuccess, written.Errors?.FirstOrDefault()?.Message);
        var read = await provider.ReadAsync(wav, TestCt);
        Assert.True(read.IsSuccess, read.Errors?.FirstOrDefault()?.Message);
        Assert.Equal("Show", read.Data!.Tags[FileTagKey.Title]);
        Assert.Equal(before, await CodecAsync(wav));
    }

    private static async Task<string> CodecAsync(string path)
    {
        var stdout = new System.Text.StringBuilder();
        await Cli.Wrap("ffprobe")
            .WithArguments("-v", "error", "-select_streams", "a:0", "-show_entries", "stream=codec_name", "-of", "default=nw=1:nk=1", path)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(TestCt);
        return stdout.ToString().Trim();
    }

    private static async Task<bool> HasToolAsync(string name)
    {
        try {
            var result = await Cli.Wrap(name).WithArguments(["-version"]).WithValidation(CommandResultValidation.None).ExecuteAsync();
            return result.ExitCode == 0;
        }
        catch (Exception) {
            return false;
        }
    }
}

file static class CliArgumentExtensions
{
    public static Command WithArguments(this Command command, params string[] args) => command.WithArguments((IEnumerable<string>)args);
}
