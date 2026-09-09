namespace Lyo.Cache;

/// <summary>Per-entry options (expiration and related settings).</summary>
public interface ICacheEntryOptions
{
    /// <summary>How long the entry lives before it expires.</summary>
    TimeSpan Duration { get; set; }

    /// <summary>
    /// Absolute expires after <see cref="Duration" /> from write. Sliding expires after <see cref="Duration" /> from the last successful access.
    /// </summary>
    CacheExpirationMode ExpirationMode { get; set; }
}
