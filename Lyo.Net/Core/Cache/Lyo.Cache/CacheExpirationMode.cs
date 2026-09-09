namespace Lyo.Cache;

/// <summary>How <see cref="ICacheEntryOptions.Duration" /> is applied to an entry.</summary>
public enum CacheExpirationMode
{
    /// <summary>Expire <see cref="ICacheEntryOptions.Duration" /> after write. Successful reads do not extend the lifetime.</summary>
    Absolute = 0,

    /// <summary>Expire <see cref="ICacheEntryOptions.Duration" /> after the last successful access. Reads reset that clock.</summary>
    Sliding = 1
}
