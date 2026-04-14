using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FractureToughness(double pascalRootMeters)
{
    public double PascalRootMeters { get; } = MathValueGuards.NonNegativeFinite(pascalRootMeters, nameof(pascalRootMeters));

    public static FractureToughness FromPascalRootMeters(double pascalRootMeters) => new(pascalRootMeters);

    public override string ToString() => $"{PascalRootMeters:0.###} Pa*sqrt(m)";
}