using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>
/// Extensible audio codec identifier. Lookups are thread-safe. Use <see cref="Custom" /> for a codec that is not listed.
/// Ids are codec tokens (<c>mp3</c>, <c>aac</c>), not a specific backend's encoder binary name.
/// </summary>
public sealed record AudioEncoder
{
    private static readonly ConcurrentDictionary<string, AudioEncoder> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, AudioEncoder> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>MP3 (<c>mp3</c>).</summary>
    public static readonly AudioEncoder Mp3 = Create("Mp3", "mp3");

    /// <summary>AAC (<c>aac</c>).</summary>
    public static readonly AudioEncoder Aac = Create("Aac", "aac");

    /// <summary>Signed 16-bit little-endian PCM (<c>pcm_s16le</c>).</summary>
    public static readonly AudioEncoder PcmS16le = Create("PcmS16le", "pcm_s16le");

    /// <summary>Opus (<c>opus</c>).</summary>
    public static readonly AudioEncoder Opus = Create("Opus", "opus");

    /// <summary>Stream copy (<c>copy</c>).</summary>
    public static readonly AudioEncoder Copy = Create("Copy", "copy");

    /// <summary>Every built-in encoder. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<AudioEncoder> BuiltIns { get; } = [Mp3, Aac, PcmS16le, Opus, Copy];

    /// <summary>Stable display name (e.g. <c>Aac</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical codec id (e.g. <c>aac</c>, <c>mp3</c>). Backends map this onto their encoder name.</summary>
    public string Id { get; }

    private AudioEncoder(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a built-in or previously created custom encoder by <see cref="Id" />.</summary>
    public static AudioEncoder? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up an encoder by display <see cref="Name" />.</summary>
    public static AudioEncoder? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns an encoder for an arbitrary codec id. Reuses a registered instance when the id is already known.</summary>
    public static AudioEncoder Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new AudioEncoder(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static AudioEncoder Create(string name, string id) => new(name, id);
}
