using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>
/// Extensible encoder speed/quality preset identifier. Lookups are thread-safe. Use <see cref="Custom" /> for a preset that is not listed.
/// </summary>
public sealed record EncoderPreset
{
    private static readonly ConcurrentDictionary<string, EncoderPreset> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, EncoderPreset> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary><c>ultrafast</c>.</summary>
    public static readonly EncoderPreset Ultrafast = Create("Ultrafast", "ultrafast");

    /// <summary><c>superfast</c>.</summary>
    public static readonly EncoderPreset Superfast = Create("Superfast", "superfast");

    /// <summary><c>veryfast</c>.</summary>
    public static readonly EncoderPreset Veryfast = Create("Veryfast", "veryfast");

    /// <summary><c>faster</c>.</summary>
    public static readonly EncoderPreset Faster = Create("Faster", "faster");

    /// <summary><c>fast</c>.</summary>
    public static readonly EncoderPreset Fast = Create("Fast", "fast");

    /// <summary><c>medium</c>.</summary>
    public static readonly EncoderPreset Medium = Create("Medium", "medium");

    /// <summary><c>slow</c>.</summary>
    public static readonly EncoderPreset Slow = Create("Slow", "slow");

    /// <summary><c>slower</c>.</summary>
    public static readonly EncoderPreset Slower = Create("Slower", "slower");

    /// <summary><c>veryslow</c>.</summary>
    public static readonly EncoderPreset Veryslow = Create("Veryslow", "veryslow");

    /// <summary>Every built-in preset. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<EncoderPreset> BuiltIns { get; } = [Ultrafast, Superfast, Veryfast, Faster, Fast, Medium, Slow, Slower, Veryslow];

    /// <summary>Stable display name (e.g. <c>Medium</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical preset id (e.g. <c>medium</c>). Backends use this as the preset name.</summary>
    public string Id { get; }

    private EncoderPreset(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a built-in or previously created custom preset by <see cref="Id" />.</summary>
    public static EncoderPreset? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a preset by display <see cref="Name" />.</summary>
    public static EncoderPreset? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a preset for an arbitrary id. Reuses a registered instance when the id is already known.</summary>
    public static EncoderPreset Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new EncoderPreset(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static EncoderPreset Create(string name, string id) => new(name, id);
}
