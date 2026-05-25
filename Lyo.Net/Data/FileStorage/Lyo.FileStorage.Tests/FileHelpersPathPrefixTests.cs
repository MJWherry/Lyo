using Lyo.Exceptions;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Pinned coverage for the shared <see cref="FileHelpers" /> path-prefix helpers. These helpers underpin both backend diagnostic listing (S3/Blob) and the core
/// <c>FileStorageServiceBase.ValidatePathPrefix</c> entry point, so any regression here cascades across every storage backend.
/// </summary>
public sealed class FileHelpersPathPrefixTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("/foo/bar/", "foo/bar")]
    [InlineData("\\foo\\bar\\", "foo\\bar")]
    public void NormalizePathPrefix_StripsWhitespaceAndSlashes(string? input, string expected)
        => Assert.Equal(expected, FileHelpers.NormalizePathPrefix(input));

    [Theory]
    [InlineData("foo/../bar")]
    [InlineData("..")]
    [InlineData("a/../../x")]
    [InlineData("a/..")]
    [InlineData("./../..")]
    [InlineData("ok//bad")]
    public void ThrowIfPathPrefixTraversal_RejectsKnownPatterns(string input)
        => Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfPathPrefixTraversal(input));

    [Fact]
    public void ThrowIfPathPrefixTraversal_RejectsEmbeddedNull()
    {
        Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfPathPrefixTraversal("foo\0bar"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo/bar/baz")]
    [InlineData("tenant_alpha/2024-05/uploads")]
    public void ThrowIfPathPrefixTraversal_AllowsLegitimateInputs(string? input)
    {
        // Helper is intentionally strict: any '..' substring is rejected (parity with FileStorageServiceBase.ValidatePathPrefix).
        FileHelpers.ThrowIfPathPrefixTraversal(input);
    }

    [Fact]
    public void NormalizeAndValidatePathPrefix_AppliesBoth()
    {
        Assert.Equal("a/b", FileHelpers.NormalizeAndValidatePathPrefix("/a/b/"));
        Assert.Equal("", FileHelpers.NormalizeAndValidatePathPrefix(null));
        Assert.Throws<ArgumentException>(() => FileHelpers.NormalizeAndValidatePathPrefix("/a/../b"));
    }

    [Fact]
    public void ThrowIfPathPrefixTraversal_CaptureCallerArgumentExpression()
    {
        var badPrefix = "a/../b";
        var ex = Assert.Throws<ArgumentException>(() => FileHelpers.ThrowIfPathPrefixTraversal(badPrefix));
        Assert.Equal(nameof(badPrefix), ex.ParamName);
    }
}
