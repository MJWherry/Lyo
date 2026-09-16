using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Channel count passed to <c>-ac</c>. Lookups are thread-safe. Use <see cref="FromCount" /> for a count that is not listed.</summary>
public sealed record ChannelLayout
{
    private static readonly ConcurrentDictionary<string, ChannelLayout> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, ChannelLayout> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>One channel.</summary>
    public static readonly ChannelLayout Mono = Create("Mono", 1);

    /// <summary>Two channels.</summary>
    public static readonly ChannelLayout Stereo = Create("Stereo", 2);

    /// <summary>Six channels.</summary>
    public static readonly ChannelLayout FiveOne = Create("FiveOne", 6);

    /// <summary>Every built-in layout. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<ChannelLayout> BuiltIns { get; } = [Mono, Stereo, FiveOne];

    /// <summary>Stable display name (e.g. <c>Stereo</c>).</summary>
    public string Name { get; }

    /// <summary>Decimal channel count.</summary>
    public string Id { get; }

    /// <summary>Channel count.</summary>
    public int Count { get; }

    private ChannelLayout(string name, int count)
    {
        ArgumentHelpers.ThrowIfLessThan(count, 1);
        Name = name;
        Count = count;
        Id = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a layout by count token.</summary>
    public static ChannelLayout? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a layout by display name.</summary>
    public static ChannelLayout? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a layout for <paramref name="count" />. Reuses a built-in when the count is already known.</summary>
    public static ChannelLayout FromCount(int count)
    {
        _ = BuiltIns;
        ArgumentHelpers.ThrowIfLessThan(count, 1);
        var key = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return NamedCatalog.GetOrCustom(ById, key, _ => new ChannelLayout(key, count));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static ChannelLayout Create(string name, int count) => new(name, count);
}
