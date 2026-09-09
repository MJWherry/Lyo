namespace Lyo.FFmpeg.Tests;

public class FFmpegPathKindTests
{
    [Fact]
    public void ThrowIfLocalFileMissing_AllowsUrlLavfiAndPipe()
    {
        FFmpegPathKind.ThrowIfLocalFileMissing("https://example.com/a.mp4");
        FFmpegPathKind.ThrowIfLocalFileMissing("sine=frequency=440:duration=1");
        FFmpegPathKind.ThrowIfLocalFileMissing("pipe:0");
        FFmpegPathKind.ThrowIfLocalFileMissing("testsrc=duration=1:size=160x120:rate=1");
    }

    [Fact]
    public void ThrowIfLocalFileMissing_MissingLocalFile_Throws()
        => Assert.Throws<FileNotFoundException>(() => FFmpegPathKind.ThrowIfLocalFileMissing(Path.Combine(Path.GetTempPath(), "lyo-ffmpeg-missing-" + Guid.NewGuid().ToString("N") + ".wav")));
}
