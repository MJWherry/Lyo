using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electric current stored in amperes.</summary>
/// <remarks>May carry a sign; must be finite.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ElectricCurrent
{
    /// <summary>SI storage scalar in Amperes (canonical unit for this ElectricCurrent).</summary>
    public double Amperes { get; }

    public ElectricCurrent(double amperes) => Amperes = MathValueGuards.Finite(amperes, nameof(amperes));

    public static ElectricCurrent FromAmperes(double amperes) => new(amperes);

    public override string ToString() => $"{Amperes:0.###} A";
}