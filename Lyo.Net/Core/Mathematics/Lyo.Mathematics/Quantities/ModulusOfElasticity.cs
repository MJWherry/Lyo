using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ModulusOfElasticity
{
    public double Pascals { get; }

    public ModulusOfElasticity(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static ModulusOfElasticity FromPascals(double pascals) => new(pascals);

    public override string ToString() => $"{Pascals:0.###} Pa";
}