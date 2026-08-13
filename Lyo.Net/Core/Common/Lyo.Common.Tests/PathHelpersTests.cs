using Lyo.Common.Pathing;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class PathHelpersTests
{
    [Fact]
    public void Combine_Posix_JoinsWithSlash() => Assert.Equal("/mem/lyo/a/b", PathHelpers.Combine(PathStyle.Posix, "/mem/lyo", "a", "b"));

    [Fact]
    public void Combine_Posix_RootedSegmentResets() => Assert.Equal("/other", PathHelpers.Combine(PathStyle.Posix, "/mem/lyo", "/other"));

    [Fact]
    public void GetFullPath_Posix_ResolvesDotDot() => Assert.Equal("/mem/lyo/b", PathHelpers.GetFullPath(PathStyle.Posix, "/mem/lyo/a/../b"));

    [Fact]
    public void GetFullPath_Posix_CannotEscapeAbsoluteRoot() => Assert.Equal("/mem", PathHelpers.GetFullPath(PathStyle.Posix, "/mem/../mem"));

    [Fact]
    public void GetFileName_Posix_ReturnsLeaf() => Assert.Equal("file.txt", PathHelpers.GetFileName(PathStyle.Posix, "/mem/lyo/file.txt"));

    [Fact]
    public void SanitizeFileName_ReplacesSlashAndInvalidChars() => Assert.Equal("a_b", PathHelpers.SanitizeFileName("a/b"));

    [Fact]
    public void SanitizeFileName_NullOrWhitespace_ReturnsNull() => Assert.Null(PathHelpers.SanitizeFileName("  "));

    [Fact]
    public void GetFileNameWithoutExtension_Posix() => Assert.Equal("file", PathHelpers.GetFileNameWithoutExtension(PathStyle.Posix, "/mem/lyo/file.txt"));

    [Fact]
    public void GetExtension_Posix() => Assert.Equal(".txt", PathHelpers.GetExtension(PathStyle.Posix, "/mem/lyo/file.txt"));

    [Fact]
    public void GetDirectoryName_Posix_Parent() => Assert.Equal("/mem/lyo", PathHelpers.GetDirectoryName(PathStyle.Posix, "/mem/lyo/file.txt"));

    [Fact]
    public void GetDirectoryName_Posix_RootFile_ReturnsRoot() => Assert.Equal("/", PathHelpers.GetDirectoryName(PathStyle.Posix, "/file.txt"));

    [Fact]
    public void IsUnderRoot_Posix_AllowsDescendant() => Assert.True(PathHelpers.IsUnderRoot(PathStyle.Posix, "/mem/lyo", "/mem/lyo/a/b"));

    [Fact]
    public void IsUnderRoot_Posix_RejectsEscape() => Assert.False(PathHelpers.IsUnderRoot(PathStyle.Posix, "/mem/lyo", "/mem/lyo/../secret"));

    [Fact]
    public void IsUnderRoot_Posix_AllowsExactRoot() => Assert.True(PathHelpers.IsUnderRoot(PathStyle.Posix, "/mem/lyo", "/mem/lyo"));

    [Fact]
    public void IsUnderRoot_Posix_FilesystemRoot_AllowsAbsoluteDescendant() => Assert.True(PathHelpers.IsUnderRoot(PathStyle.Posix, "/", "/upload/roundtrip.txt"));

    [Fact]
    public void ThrowIfEscapesRoot_Posix_Throws()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => PathHelpers.ThrowIfEscapesRoot(PathStyle.Posix, "/mem/lyo", "/mem/other"));
        Assert.Contains("escapes root", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_ThrowsArgument() => Assert.Throws<ArgumentException>(() => PathHelpers.ThrowIfNullOrWhiteSpace("   "));

    [Fact]
    public void ThrowIfInvalidPath_Posix_RejectsNul() => Assert.Throws<InvalidFormatException>(() => PathHelpers.ThrowIfInvalidPath("a\0b", PathStyle.Posix));

    [Fact]
    public void Combine_Host_MatchesSystemPath()
    {
        var expected = Path.Combine(Path.GetTempPath(), "lyo-path-test", "x");
        var actual = PathHelpers.Combine(PathStyle.Host, Path.GetTempPath(), "lyo-path-test", "x");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetFullPath_Host_MatchesSystemPath()
    {
        var relative = Path.Combine(".", "lyo-path-helpers-test");
        Assert.Equal(Path.GetFullPath(relative), PathHelpers.GetFullPath(PathStyle.Host, relative));
    }

    [Fact]
    public void NormalizeSeparators_Posix_ConvertsBackslash() => Assert.Equal("/a/b", PathHelpers.NormalizeSeparators(@"\a\b", PathStyle.Posix));

    [Fact]
    public void TrimTrailingSeparators_Posix_KeepsRoot() => Assert.Equal("/", PathHelpers.TrimTrailingSeparators("///", PathStyle.Posix));
}