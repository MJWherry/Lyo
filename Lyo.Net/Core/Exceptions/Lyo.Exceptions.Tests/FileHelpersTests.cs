namespace Lyo.Exceptions.Tests;

public class FileHelpersTests
{
    [Fact]
    public void ThrowIfFileNameInvalid_SimpleName_DoesNotThrow() => FileHelpers.ThrowIfFileNameInvalid("document.pdf");

    [Fact]
    public void ThrowIfFileNameInvalid_Null_ThrowsArgumentNull()
    {
        string? fileName = null;
        Assert.Throws<ArgumentNullException>(() => FileHelpers.ThrowIfFileNameInvalid(fileName));
    }

    [Fact]
    public void ThrowIfFileNameInvalid_PathTraversal_Throws()
        => Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfFileNameInvalid("../evil.txt"));

    [Fact]
    public void ThrowIfFileNameInvalid_AbsolutePath_Throws()
        => Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfFileNameInvalid(Path.Combine(Path.GetTempPath(), "file.txt")));

    [Fact]
    public void ThrowIfFileNameInvalid_InvalidCharacter_Throws()
        => Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfFileNameInvalid("bad\0name.txt"));

    [Fact]
    public void GetValidFileName_PlainName_ReturnsIt() => Assert.Equal("document.pdf", FileHelpers.GetValidFileName("document.pdf"));

    [Fact]
    public void GetValidFileName_Path_ReturnsFinalSegment() => Assert.Equal("file.txt", FileHelpers.GetValidFileName("some/dir/file.txt"));

    [Fact]
    public void GetValidFileName_Whitespace_Throws()
        => Assert.Throws<ArgumentException>(() => FileHelpers.GetValidFileName("   "));

    [Fact]
    public void TryGetValidFileName_Valid_ReturnsTrueWithName()
    {
        Assert.True(FileHelpers.TryGetValidFileName("dir/photo.jpg", out var fileName));
        Assert.Equal("photo.jpg", fileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("dir/..")]
    public void TryGetValidFileName_Invalid_ReturnsFalse(string? value)
    {
        Assert.False(FileHelpers.TryGetValidFileName(value, out var fileName));
        Assert.Null(fileName);
    }

    [Fact]
    public void TryGetValidFileName_RootedPath_ReturnsFalse()
    {
        Assert.False(FileHelpers.TryGetValidFileName(Path.Combine(Path.GetTempPath(), "file.txt"), out var fileName));
        Assert.Null(fileName);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("/tenant/alpha/", "tenant/alpha")]
    [InlineData(" /a/b ", "a/b")]
    [InlineData("plain", "plain")]
    public void NormalizePathPrefix_ReturnsTrimmedPrefix(string? value, string expected)
        => Assert.Equal(expected, FileHelpers.NormalizePathPrefix(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a/b")]
    public void ThrowIfPathPrefixTraversal_Safe_DoesNotThrow(string? value) => FileHelpers.ThrowIfPathPrefixTraversal(value);

    [Theory]
    [InlineData("a/../b")]
    [InlineData("a//b")]
    [InlineData("a\\\\b")]
    [InlineData("a\0b")]
    public void ThrowIfPathPrefixTraversal_Traversal_Throws(string value)
        => Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfPathPrefixTraversal(value));

    [Fact]
    public void NormalizeAndValidatePathPrefix_Valid_ReturnsTrimmed()
        => Assert.Equal("a/b", FileHelpers.NormalizeAndValidatePathPrefix("/a/b/"));

    [Fact]
    public void NormalizeAndValidatePathPrefix_Traversal_Throws()
        => Assert.Throws<ArgumentException>(() => FileHelpers.NormalizeAndValidatePathPrefix("/a/../b/"));
}
