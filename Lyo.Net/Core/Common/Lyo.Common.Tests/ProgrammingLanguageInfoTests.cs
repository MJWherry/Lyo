using Lyo.Common.Records;

namespace Lyo.Common.Tests;

public class ProgrammingLanguageInfoTests
{
    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        var info = ProgrammingLanguageInfo.CSharp;
        Assert.Equal("C#", info.Name);
        Assert.Equal("cs", info.ShortName);
        Assert.Equal("csharp", info.Slug);
        Assert.Equal(".NET", info.RuntimeFamily);
        Assert.Contains(".cs", info.FileExtensions);
        Assert.True(info.IsCompiled);
        Assert.False(info.IsInterpreted);
    }

    [Theory]
    [InlineData("C#")]
    [InlineData("csharp")]
    [InlineData("dotnet")]
    public void FromAlias_ResolvesCommonNames(string alias)
    {
        var info = ProgrammingLanguageInfo.FromAlias(alias);
        Assert.Equal(ProgrammingLanguageInfo.CSharp, info);
    }

    [Theory]
    [InlineData(".ts", "ts")]
    [InlineData("py", "py")]
    [InlineData(".html", "html")]
    public void FromExtension_ResolvesKnownExtensions(string extension, string expectedShortName)
    {
        var info = ProgrammingLanguageInfo.FromExtension(extension);
        Assert.Equal(expectedShortName, info.ShortName);
    }

    [Theory]
    [InlineData("cs", "C#")]
    [InlineData("py", "Python")]
    [InlineData("ps1", "PowerShell")]
    public void FromShortName_ResolvesCanonicalValues(string shortName, string expectedName)
    {
        var info = ProgrammingLanguageInfo.FromShortName(shortName);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(shortName, info.ShortName);
    }

    [Theory]
    [InlineData("cs")]
    [InlineData("py")]
    [InlineData("ps1")]
    [InlineData("jl")]
    [InlineData("zig")]
    public void FromName_DoesNotResolveShortNames(string shortName)
    {
        var info = ProgrammingLanguageInfo.FromName(shortName);
        Assert.Equal(ProgrammingLanguageInfo.Unknown, info);
    }

    [Theory]
    [InlineData("fs", "F#")]
    [InlineData("vb", "Visual Basic")]
    [InlineData("ps1", "PowerShell")]
    [InlineData("jl", "Julia")]
    [InlineData("zig", "Zig")]
    public void AdditionalLanguages_HaveExpectedShortNames(string shortName, string expectedName)
    {
        var info = ProgrammingLanguageInfo.FromShortName(shortName);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(shortName, info.ShortName);
    }
}