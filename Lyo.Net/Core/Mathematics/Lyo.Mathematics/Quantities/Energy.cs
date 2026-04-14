using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Energy(double joules)
{
    public double Joules { get; } = MathValueGuards.Finite(joules, nameof(joules));

    public static Energy FromJoules(double joules) => new(joules);

    public override string ToString() => $"{Joules:0.###} J";
}