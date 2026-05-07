using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MassFlowRate
{

    public MassFlowRate(double kilogramsPerSecond)

    {

        KilogramsPerSecond = MathValueGuards.NonNegativeFinite(kilogramsPerSecond, nameof(kilogramsPerSecond));

    }


    public double KilogramsPerSecond { get;  }
    public static MassFlowRate FromKilogramsPerSecond(double kilogramsPerSecond) => new(kilogramsPerSecond);

    public override string ToString() => $"{KilogramsPerSecond:0.###} kg/s";
}