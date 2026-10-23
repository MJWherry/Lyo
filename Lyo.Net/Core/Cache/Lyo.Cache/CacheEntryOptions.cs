using System.Diagnostics;

namespace Lyo.Cache;

/// <summary>Mutable <see cref="ICacheEntryOptions" /> used with <c>IMemoryCache</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CacheEntryOptions : ICacheEntryOptions
{
    public TimeSpan Duration { get; set; }

    public CacheExpirationMode ExpirationMode { get; set; } = CacheExpirationMode.Absolute;

    /// <summary>Duration and expiration mode.</summary>
    public override string ToString() => $"Duration={Duration} ExpirationMode={ExpirationMode}";
}