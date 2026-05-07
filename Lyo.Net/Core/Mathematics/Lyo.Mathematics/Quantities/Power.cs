using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Power
{
    public double Watts { get; }

    public Power(double watts) => Watts = MathValueGuards.Finite(watts, nameof(watts));

    public static Power FromWatts(double watts) => new(watts);

    public override string ToString() => $"{Watts:0.###} W";
}