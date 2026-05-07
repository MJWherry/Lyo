using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FractureToughness
{

    public FractureToughness(double pascalRootMeters)

    {

        PascalRootMeters = MathValueGuards.NonNegativeFinite(pascalRootMeters, nameof(pascalRootMeters));

    }


    public double PascalRootMeters { get;  }
    public static FractureToughness FromPascalRootMeters(double pascalRootMeters) => new(pascalRootMeters);

    public override string ToString() => $"{PascalRootMeters:0.###} Pa*sqrt(m)";
}