using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Tests;

/// <summary>Tests for SharedEntityMetadataCache.NormalizeFieldPath, including the .count keyword for projections.</summary>
public class ProjectionNormalizeFieldPathTests
{
    [Fact]
    public void NormalizeFieldPath_WithCountKeyword_ResolvesCollectionCountPath()
    {
        var result = SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Tags.Count");
        Assert.Equal("Tags.count", result);
    }

    [Fact]
    public void NormalizeFieldPath_WithCountKeyword_CaseInsensitive()
    {
        var result = SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Tags.COUNT");
        Assert.Equal("Tags.count", result);
    }

    [Fact]
    public void NormalizeFieldPath_WithCountKeyword_CachesResult()
    {
        var first = SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Tags.Count");
        var second = SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Tags.Count");
        Assert.Equal(first, second);
    }

    [Fact]
    public void NormalizeFieldPath_WithCountKeyword_ThrowsWhenNotCollection()
        => Assert.Throws<ArgumentException>(() => SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Name.Count"));
}