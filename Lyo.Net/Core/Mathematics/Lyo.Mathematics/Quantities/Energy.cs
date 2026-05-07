using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Energy
{
    public double Joules { get; }

    public Energy(double joules) => Joules = MathValueGuards.Finite(joules, nameof(joules));

    public static Energy FromJoules(double joules) => new(joules);

    public override string ToString() => $"{Joules:0.###} J";
}