using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>
/// Extensible video codec identifier. Lookups are thread-safe. Use <see cref="Custom" /> for a codec that is not listed.
/// Ids are codec tokens (<c>h264</c>, <c>vp9</c>), not a specific backend's encoder binary name.
/// </summary>
public sealed record VideoEncoder
{
    private static readonly ConcurrentDictionary<string, VideoEncoder> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, VideoEncoder> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>H.264 (<c>h264</c>).</summary>
    public static readonly VideoEncoder H264 = Create("H264", "h264");

    /// <summary>MPEG-4 Part 2 (<c>mpeg4</c>).</summary>
    public static readonly VideoEncoder Mpeg4 = Create("Mpeg4", "mpeg4");

    /// <summary>VP9 (<c>vp9</c>).</summary>
    public static readonly VideoEncoder Vp9 = Create("Vp9", "vp9");

    /// <summary>Stream copy (<c>copy</c>).</summary>
    public static readonly VideoEncoder Copy = Create("Copy", "copy");

    /// <summary>Every built-in encoder. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<VideoEncoder> BuiltIns { get; } = [H264, Mpeg4, Vp9, Copy];

    /// <summary>Stable display name (e.g. <c>H264</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical codec id (e.g. <c>h264</c>, <c>mpeg4</c>). Backends map this onto their encoder name.</summary>
    public string Id { get; }

    private VideoEncoder(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a built-in or previously created custom encoder by <see cref="Id" />.</summary>
    public static VideoEncoder? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up an encoder by display <see cref="Name" />.</summary>
    public static VideoEncoder? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns an encoder for an arbitrary codec id. Reuses a registered instance when the id is already known.</summary>
    public static VideoEncoder Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new VideoEncoder(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static VideoEncoder Create(string name, string id) => new(name, id);
}
