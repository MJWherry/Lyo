using Lyo.FileTags.FFmpeg;
using Lyo.FileTags.Images;
using Lyo.FFmpeg.Models;

namespace Lyo.FileTags.Tests;

public class FileTagServiceTests
{
    [Fact]
    public async Task Service_NoProvider_ReturnsFailure()
    {
        var ffmpeg = new FFmpegFileTagProvider(new());
        var images = new ImageFileTagProvider();
        var service = new FileTagService([ffmpeg, images], [ffmpeg, images]);
        Assert.False(service.CanRead("notes.pdf"));
        Assert.False(service.CanWrite("notes.pdf"));
        var result = await service.ReadAsync("notes.pdf", TestCt);
        Assert.False(result.IsSuccess);
    }
}
