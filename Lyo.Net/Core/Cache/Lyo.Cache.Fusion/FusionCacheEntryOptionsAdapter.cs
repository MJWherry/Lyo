using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Fusion;

/// <summary>Maps <c>FusionCacheEntryOptions</c> onto <see cref="ICacheEntryOptions" />.</summary>
internal sealed class FusionCacheEntryOptionsAdapter : ICacheEntryOptions
{
    private readonly FusionCacheEntryOptions _inner;

    public FusionCacheEntryOptionsAdapter(FusionCacheEntryOptions inner) => _inner = inner;

    public TimeSpan Duration {
        get => _inner.Duration;
        set => _inner.Duration = value;
    }

    public CacheExpirationMode ExpirationMode { get; set; } = CacheExpirationMode.Absolute;
}