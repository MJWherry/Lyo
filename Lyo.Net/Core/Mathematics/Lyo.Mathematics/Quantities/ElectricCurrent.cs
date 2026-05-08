using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electric current stored in amperes.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ElectricCurrent
{
    /// <summary>Canonical SI scalar in Amperes (storage for this ElectricCurrent).</summary>
    public double Amperes { get; }

    public ElectricCurrent(double amperes) => Amperes = MathValueGuards.Finite(amperes, nameof(amperes));

    public static ElectricCurrent FromAmperes(double amperes) => new(amperes);

    public override string ToString() => $"{Amperes:0.###} A";
}