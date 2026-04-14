namespace Lyo.Cache;

/// <summary>Mutable implementation of ICacheEntryOptions for use with IMemoryCache.</summary>
public sealed class CacheEntryOptions : ICacheEntryOptions
{
    public TimeSpan Duration { get; set; }
}