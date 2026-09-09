using Lyo.FileSystemWatcher.Models;
using Lyo.Testing;

namespace Lyo.FileSystemWatcher.Tests;

public class FileSystemPathGlobTests
{
    [Theory]
    [InlineData("app.json", @"\.json$", true)]
    [InlineData("src/app.json", @"\.json$", true)]
    [InlineData("src/app.txt", @"\.json$", false)]
    [InlineData("app.json", @"^[^/]*\.json$", true)]
    [InlineData("src/app.json", @"^[^/]*\.json$", false)]
    [InlineData(".git/config", @"(^|/)\.git(/|$)", true)]
    [InlineData("src/.git/config", @"(^|/)\.git(/|$)", true)]
    [InlineData("src/file.txt", @"(^|/)\.git(/|$)", false)]
    [InlineData("bin/Debug/a.dll", @"(^|/)bin(/|$)", true)]
    [InlineData("readme.md", @"^readme\.md$", true)]
    [InlineData("Readme.md", @"^readme\.md$", true)]
    public void IsMatch_Pattern_ExpectedResult(string path, string pattern, bool expected)
        => FileSystemPathGlob.IsMatch(path, pattern).ShouldBe(expected);

    [Fact]
    public void IsFileIncluded_EmptyInclude_MatchesAll()
        => FileSystemPathGlob.IsFileIncluded("a.txt", [], [@"(^|/)\.git(/|$)"]).ShouldBeTrue();

    [Fact]
    public void IsFileIncluded_ExcludeWins()
        => FileSystemPathGlob.IsFileIncluded("bin/a.dll", [@".*"], [@"(^|/)bin(/|$)"]).ShouldBeFalse();

    [Fact]
    public void IsFileIncluded_IncludeFilters()
    {
        FileSystemPathGlob.IsFileIncluded("a.json", [@"\.json$"], []).ShouldBeTrue();
        FileSystemPathGlob.IsFileIncluded("a.txt", [@"\.json$"], []).ShouldBeFalse();
    }

    [Fact]
    public void IsDirectoryExcluded_GitTree_IsExcluded()
        => FileSystemPathGlob.IsDirectoryExcluded(".git", [@"(^|/)\.git(/|$)"]).ShouldBeTrue();

    [Fact]
    public void IsDirectoryExcluded_Src_IsNotExcluded()
        => FileSystemPathGlob.IsDirectoryExcluded("src", [@"(^|/)\.git(/|$)"]).ShouldBeFalse();

    [Fact]
    public void IsDirectoryExcluded_Root_IsNeverExcluded()
        => FileSystemPathGlob.IsDirectoryExcluded("", [@".*"]).ShouldBeFalse();

    [Fact]
    public void Normalize_Backslashes_BecomeSlash()
        => FileSystemPathGlob.Normalize(@"src\app.json").ShouldBe("src/app.json");

    [Fact]
    public void ValidatePatterns_InvalidRegex_Throws()
        => Assert.Throws<ArgumentException>(() => FileSystemPathGlob.ValidatePatterns(["*.txt"]));
}
