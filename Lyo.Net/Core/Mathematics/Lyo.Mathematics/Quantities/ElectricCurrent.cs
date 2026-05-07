using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ElectricCurrent
{
    public double Amperes { get; }

    public ElectricCurrent(double amperes) => Amperes = MathValueGuards.Finite(amperes, nameof(amperes));

    public static ElectricCurrent FromAmperes(double amperes) => new(amperes);

    public override string ToString() => $"{Amperes:0.###} A";
}