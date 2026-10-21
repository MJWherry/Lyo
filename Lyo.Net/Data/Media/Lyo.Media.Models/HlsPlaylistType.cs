using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>HLS playlist type passed to <c>-hls_playlist_type</c>. Lookups are thread-safe.</summary>
public sealed record HlsPlaylistType
{
    private static readonly ConcurrentDictionary<string, HlsPlaylistType> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, HlsPlaylistType> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Video on demand (<c>vod</c>).</summary>
    public static readonly HlsPlaylistType Vod = Create("Vod", "vod");

    /// <summary>Event playlist (<c>event</c>).</summary>
    public static readonly HlsPlaylistType Event = Create("Event", "event");

    /// <summary>Live playlist (<c>live</c>).</summary>
    public static readonly HlsPlaylistType Live = Create("Live", "live");

    /// <summary>Every built-in playlist type. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<HlsPlaylistType> BuiltIns { get; } = [Vod, Event, Live];

    /// <summary>Stable display name (e.g. <c>Vod</c>).</summary>
    public string Name { get; }

    /// <summary>FFmpeg playlist token.</summary>
    public string Id { get; }

    private HlsPlaylistType(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a playlist type by <see cref="Id" />.</summary>
    public static HlsPlaylistType? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a playlist type by display name.</summary>
    public static HlsPlaylistType? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a playlist type for an arbitrary token. Reuses a registered instance when the token is already known.</summary>
    public static HlsPlaylistType Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static HlsPlaylistType Create(string name, string id) => new(name, id);
}