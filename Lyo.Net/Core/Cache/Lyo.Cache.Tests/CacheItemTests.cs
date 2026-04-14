namespace Lyo.Cache.Tests;

public class CacheItemTests
{
    [Fact]
    public void CacheItem_WithSameNameAndTypeButDifferentCreated_AreEqual()
    {
        var item1 = CacheItem.Tag("__tag:entity:personentity", new DateTime(2026, 3, 8, 21, 36, 0, DateTimeKind.Utc));
        var item2 = CacheItem.Tag("__tag:entity:personentity", new DateTime(2026, 3, 8, 21, 42, 0, DateTimeKind.Utc));
        Assert.True(item1.Equals(item2));
        Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
    }

    [Fact]
    public void CacheItem_NameComparison_IsCaseInsensitive()
    {
        var item1 = CacheItem.Tag("__tag:entity:personentity");
        var item2 = CacheItem.Tag("__TAG:ENTITY:PERSONENTITY");
        Assert.True(item1.Equals(item2));
        Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
    }
}