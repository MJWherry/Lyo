using System.Diagnostics;

namespace Lyo.Scientific.Units;

/// <summary>Metric prefix metadata: display name, symbol, and numeric multiplier applied to a base unit.</summary>
/// <param name="Name">Full prefix name (for example <c>Kilo</c>).</param>
/// <param name="Symbol">Abbreviated symbol (for example <c>k</c>).</param>
/// <param name="Multiplier">Factor applied when scaling values (for example <c>1e3</c> for kilo).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ScientificUnitPrefix(string Name, string Symbol, double Multiplier)
{
    /// <inheritdoc />
    public override string ToString() => $"{Symbol} ({Name}), ×{Multiplier}";
}

/// <summary>Common SI metric prefixes from tera down to pico.</summary>
public static class ScientificUnitPrefixes
{
    /// <summary>Ordered list of metric prefixes suitable for UI pickers or scaling helpers.</summary>
    public static IReadOnlyList<ScientificUnitPrefix> Metric { get; } = [
        new("Tera", "T", 1e12), new("Giga", "G", 1e9), new("Mega", "M", 1e6), new("Kilo", "k", 1e3), new("Hecto", "h", 1e2), new("Deca", "da", 1e1), new("Deci", "d", 1e-1),
        new("Centi", "c", 1e-2), new("Milli", "m", 1e-3), new("Micro", "u", 1e-6), new("Nano", "n", 1e-9), new("Pico", "p", 1e-12)
    ];
}