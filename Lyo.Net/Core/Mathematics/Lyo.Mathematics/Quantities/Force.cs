using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Force(double newtons)
{
    public double Newtons { get; } = MathValueGuards.Finite(newtons, nameof(newtons));

    public static Force FromNewtons(double newtons) => new(newtons);

    public override string ToString() => $"{Newtons:0.###} N";
}