namespace Lyo.FileTags.Tests;

public class FileTagKeyTests
{
    [Fact]
    public void FileTagKey_Title_IdIsTitle()
    {
        Assert.Equal("title", FileTagKey.Title.Id);
        var composer = FileTagKey.Custom("composer");
        Assert.Equal("composer", composer.Id);
        Assert.Same(composer, FileTagKey.TryFromId("composer"));
    }
}
