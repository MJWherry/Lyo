using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DynamicViscosity(double pascalSeconds)
{
    public double PascalSeconds { get; } = MathValueGuards.NonNegativeFinite(pascalSeconds, nameof(pascalSeconds));

    public static DynamicViscosity FromPascalSeconds(double pascalSeconds) => new(pascalSeconds);

    public override string ToString() => $"{PascalSeconds:0.###} Pa*s";
}