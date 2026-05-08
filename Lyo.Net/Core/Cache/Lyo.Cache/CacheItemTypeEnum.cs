namespace Lyo.Cache;

/// <summary>Discriminator for <see cref="CacheItem" /> rows exposed via <see cref="ICacheService.Items" />.</summary>
public enum CacheItemTypeEnum
{
    /// <summary>A normalized cache entry key.</summary>
    Key,

    /// <summary>A tag string present in the tag index.</summary>
    Tag
}