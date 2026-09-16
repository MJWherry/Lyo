using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Tests;

/// <summary>Coverage for SharedEntityMetadataCache.NormalizeFieldPath, including the .count keyword for projections.</summary>
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
        => Assert.Throws<ArgumentNullException>(() => SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Name.Count"));

    [Fact]
    public void NormalizeFieldPath_Index_KeepsBracketsOnCanonicalName()
    {
        Assert.Equal("Tags[0]", SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "tags[0]"));
        Assert.Equal("Name[0:]", SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "name[0:]"));
        Assert.Equal("Name[:3]", SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "name[:3]"));
        Assert.Equal("Name[0:3]", SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "name[0:3]"));
    }

    [Fact]
    public void NormalizeFieldPath_IndexOnNumber_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => SharedEntityMetadataCache.NormalizeFieldPath(typeof(Person), "Age[0]"));
        Assert.Contains("string or a collection", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}