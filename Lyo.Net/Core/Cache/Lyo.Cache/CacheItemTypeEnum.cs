namespace Lyo.Cache;

/// <summary>Kind of <see cref="CacheItem" /> row listed by <see cref="ICacheService.Items" />.</summary>
public enum CacheItemTypeEnum
{
    /// <summary>Normalized cache entry key.</summary>
    Key,

    /// <summary>Tag string present in the tag index.</summary>
    Tag
}