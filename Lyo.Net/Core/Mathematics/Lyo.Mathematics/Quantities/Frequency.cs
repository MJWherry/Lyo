using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Frequency(double hertz)
{
    public double Hertz { get; } = MathValueGuards.NonNegativeFinite(hertz, nameof(hertz));

    public static Frequency FromHertz(double hertz) => new(hertz);

    public override string ToString() => $"{Hertz:0.###} Hz";
}