using System.Collections.Concurrent;
using System.Diagnostics;

namespace Lyo.Media.Models;

/// <summary>VBR quality digit for <c>-q:a</c>. <see cref="V0" /> is the highest quality. Lookups are thread-safe.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AudioVbrQuality
{
    private static readonly ConcurrentDictionary<string, AudioVbrQuality> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, AudioVbrQuality> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Highest VBR quality (<c>0</c>).</summary>
    public static readonly AudioVbrQuality V0 = Create("V0", "0");

    /// <summary><c>1</c>.</summary>
    public static readonly AudioVbrQuality V1 = Create("V1", "1");

    /// <summary><c>2</c>.</summary>
    public static readonly AudioVbrQuality V2 = Create("V2", "2");

    /// <summary><c>3</c>.</summary>
    public static readonly AudioVbrQuality V3 = Create("V3", "3");

    /// <summary><c>4</c>.</summary>
    public static readonly AudioVbrQuality V4 = Create("V4", "4");

    /// <summary><c>5</c>.</summary>
    public static readonly AudioVbrQuality V5 = Create("V5", "5");

    /// <summary><c>6</c>.</summary>
    public static readonly AudioVbrQuality V6 = Create("V6", "6");

    /// <summary><c>7</c>.</summary>
    public static readonly AudioVbrQuality V7 = Create("V7", "7");

    /// <summary><c>8</c>.</summary>
    public static readonly AudioVbrQuality V8 = Create("V8", "8");

    /// <summary>Lowest VBR quality (<c>9</c>).</summary>
    public static readonly AudioVbrQuality V9 = Create("V9", "9");

    /// <summary>Every built-in quality. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<AudioVbrQuality> BuiltIns { get; } = [V0, V1, V2, V3, V4, V5, V6, V7, V8, V9];

    /// <summary>Stable display name (e.g. <c>V2</c>).</summary>
    public string Name { get; }

    /// <summary>Digit passed to <c>-q:a</c>.</summary>
    public string Id { get; }

    private AudioVbrQuality(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a quality by <see cref="Id" />.</summary>
    public static AudioVbrQuality? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a quality by display <see cref="Name" />.</summary>
    public static AudioVbrQuality? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a quality for an arbitrary digit. Reuses a registered instance when the id is already known.</summary>
    public static AudioVbrQuality Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static AudioVbrQuality Create(string name, string id) => new(name, id);
}