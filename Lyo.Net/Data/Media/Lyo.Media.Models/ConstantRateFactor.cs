using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Constant rate factor. Hardware encoders remap <see cref="Value" /> onto their own quality flag. Lookups are thread-safe.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ConstantRateFactor
{
    private static readonly ConcurrentDictionary<string, ConstantRateFactor> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, ConstantRateFactor> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>CRF 18.</summary>
    public static readonly ConstantRateFactor VisuallyLossless = Create("VisuallyLossless", 18);

    /// <summary>CRF 20.</summary>
    public static readonly ConstantRateFactor High = Create("High", 20);

    /// <summary>CRF 23, the usual x264 default.</summary>
    public static readonly ConstantRateFactor Standard = Create("Standard", 23);

    /// <summary>CRF 28.</summary>
    public static readonly ConstantRateFactor Smaller = Create("Smaller", 28);

    /// <summary>Every built-in factor. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<ConstantRateFactor> BuiltIns { get; } = [VisuallyLossless, High, Standard, Smaller];

    /// <summary>Stable display name (e.g. <c>Standard</c>).</summary>
    public string Name { get; }

    /// <summary>Decimal CRF token.</summary>
    public string Id { get; }

    /// <summary>CRF number passed to the encoder.</summary>
    public int Value { get; }

    private ConstantRateFactor(string name, int value)
    {
        ArgumentHelpers.ThrowIfLessThan(value, 0);
        Name = name;
        Value = value;
        Id = value.ToString(CultureInfo.InvariantCulture);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a factor by numeric token.</summary>
    public static ConstantRateFactor? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a factor by display name.</summary>
    public static ConstantRateFactor? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a factor for <paramref name="value" />. Reuses a built-in when the value is already known.</summary>
    public static ConstantRateFactor FromValue(int value)
    {
        _ = BuiltIns;
        ArgumentHelpers.ThrowIfLessThan(value, 0);
        var key = value.ToString(CultureInfo.InvariantCulture);
        return NamedCatalog.GetOrCustom(ById, key, _ => new(key, value));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static ConstantRateFactor Create(string name, int value) => new(name, value);
}