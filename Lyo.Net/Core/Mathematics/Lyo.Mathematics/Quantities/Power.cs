using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Power(double watts)
{
    public double Watts { get; } = MathValueGuards.Finite(watts, nameof(watts));

    public static Power FromWatts(double watts) => new(watts);

    public override string ToString() => $"{Watts:0.###} W";
}