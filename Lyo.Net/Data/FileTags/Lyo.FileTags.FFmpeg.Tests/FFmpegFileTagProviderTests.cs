namespace Lyo.FileTags.FFmpeg.Tests;

public class FFmpegFileTagProviderTests
{
    [Fact]
    public void Read_FormatTagWinsOverStreamTag()
    {
        const string json = """
            {
              "streams": [
                { "tags": { "title": "Other", "artist": "Band", "composer": "Ignored" } }
              ],
              "format": {
                "tags": { "title": "Show", "composer": "Ada" }
              }
            }
            """;

        var tags = FFmpegTagJson.Parse(json);
        Assert.Equal("Show", tags.Tags[FileTagKey.Title]);
        Assert.Equal("Band", tags.Tags[FileTagKey.Artist]);
        Assert.Equal("Ada", tags.Tags[FileTagKey.Custom("composer")]);
        Assert.NotEqual(FileTagKey.Title, FileTagKey.Custom("composer"));
    }

    [Fact]
    public void Write_EmitsStreamCopyAndMetadata()
    {
        var request = new FileTagWriteRequest(new Dictionary<FileTagKey, string> { [FileTagKey.Title] = "Show" });
        var args = FFmpegFileTagProvider.BuildWriteArguments("in.wav", "out.wav", request).ToList();
        Assert.Equal("copy", args[args.IndexOf("-c") + 1]);
        Assert.Equal("title=Show", args[args.IndexOf("-metadata") + 1]);
        Assert.DoesNotContain("libx264", args);

        var cleared = new FileTagWriteRequest(request.Tags, clearExisting: true);
        var clearedArgs = FFmpegFileTagProvider.BuildWriteArguments("in.wav", "out.wav", cleared).ToList();
        var mapAt = clearedArgs.IndexOf("-map_metadata");
        Assert.Equal("-1", clearedArgs[mapAt + 1]);
        Assert.True(mapAt < clearedArgs.IndexOf("-metadata"));
        Assert.True(mapAt < clearedArgs.IndexOf("-c"));
    }
}
