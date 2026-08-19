namespace Lyo.Cache;

/// <summary>How a cache entry's <see cref="ICacheEntryOptions.Duration" /> is applied.</summary>
public enum CacheExpirationMode
{
    /// <summary>Expire <see cref="ICacheEntryOptions.Duration" /> after write. Successful reads do not extend lifetime.</summary>
    Absolute = 0,

    /// <summary>Expire <see cref="ICacheEntryOptions.Duration" /> after the last successful access. Reads reset the clock.</summary>
    Sliding = 1
}
