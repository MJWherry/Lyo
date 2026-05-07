using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MomentOfInertia
{

    public MomentOfInertia(double kilogramSquareMeters)

    {

        KilogramSquareMeters = MathValueGuards.NonNegativeFinite(kilogramSquareMeters, nameof(kilogramSquareMeters));

    }


    public double KilogramSquareMeters { get;  }
    public static MomentOfInertia FromKilogramSquareMeters(double kilogramSquareMeters) => new(kilogramSquareMeters);

    public override string ToString() => $"{KilogramSquareMeters:0.###} kg*m^2";
}