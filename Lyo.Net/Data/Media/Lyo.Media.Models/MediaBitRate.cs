using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>Audio or video bitrate token passed to ffmpeg (<c>128k</c>, <c>2M</c>). Lookups are thread-safe. Use <see cref="Custom" /> for a rate that is not listed.</summary>
public sealed record MediaBitRate
{
    private static readonly ConcurrentDictionary<string, MediaBitRate> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, MediaBitRate> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>64 kbps.</summary>
    public static readonly MediaBitRate K64 = Create("K64", "64k");

    /// <summary>96 kbps.</summary>
    public static readonly MediaBitRate K96 = Create("K96", "96k");

    /// <summary>128 kbps.</summary>
    public static readonly MediaBitRate K128 = Create("K128", "128k");

    /// <summary>192 kbps.</summary>
    public static readonly MediaBitRate K192 = Create("K192", "192k");

    /// <summary>256 kbps.</summary>
    public static readonly MediaBitRate K256 = Create("K256", "256k");

    /// <summary>320 kbps.</summary>
    public static readonly MediaBitRate K320 = Create("K320", "320k");

    /// <summary>1 Mbps.</summary>
    public static readonly MediaBitRate M1 = Create("M1", "1M");

    /// <summary>2 Mbps.</summary>
    public static readonly MediaBitRate M2 = Create("M2", "2M");

    /// <summary>5 Mbps.</summary>
    public static readonly MediaBitRate M5 = Create("M5", "5M");

    /// <summary>8 Mbps.</summary>
    public static readonly MediaBitRate M8 = Create("M8", "8M");

    /// <summary>Every built-in rate. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<MediaBitRate> BuiltIns { get; } = [K64, K96, K128, K192, K256, K320, M1, M2, M5, M8];

    /// <summary>Stable display name (e.g. <c>K128</c>).</summary>
    public string Name { get; }

    /// <summary>FFmpeg bitrate token (e.g. <c>128k</c>).</summary>
    public string Id { get; }

    private MediaBitRate(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a rate by <see cref="Id" />.</summary>
    public static MediaBitRate? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a rate by display <see cref="Name" />.</summary>
    public static MediaBitRate? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a rate for an arbitrary token such as <c>200k</c>. Reuses a registered instance when the token is already known.</summary>
    public static MediaBitRate Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static MediaBitRate Create(string name, string id) => new(name, id);
}