namespace Lyo.Cache;

/// <summary>Mutable <see cref="ICacheEntryOptions" /> used with <c>IMemoryCache</c>.</summary>
public sealed class CacheEntryOptions : ICacheEntryOptions
{
    public TimeSpan Duration { get; set; }

    public CacheExpirationMode ExpirationMode { get; set; } = CacheExpirationMode.Absolute;
}
