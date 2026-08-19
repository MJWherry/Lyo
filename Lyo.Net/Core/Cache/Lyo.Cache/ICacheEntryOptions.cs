namespace Lyo.Cache;

/// <summary>Options for configuring a cache entry (expiration, etc.).</summary>
public interface ICacheEntryOptions
{
    /// <summary>Duration before the cache entry expires.</summary>
    TimeSpan Duration { get; set; }

    /// <summary>
    /// Absolute expires after <see cref="Duration" /> from write. Sliding expires after <see cref="Duration" /> from last successful access.
    /// </summary>
    CacheExpirationMode ExpirationMode { get; set; }
}
