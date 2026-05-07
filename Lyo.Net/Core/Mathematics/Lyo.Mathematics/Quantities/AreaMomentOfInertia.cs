using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AreaMomentOfInertia
{
    public double MetersToFourth { get; }

    public AreaMomentOfInertia(double metersToFourth) => MetersToFourth = MathValueGuards.NonNegativeFinite(metersToFourth, nameof(metersToFourth));

    public static AreaMomentOfInertia FromMetersToFourth(double metersToFourth) => new(metersToFourth);

    public override string ToString() => $"{MetersToFourth:0.###} m^4";
}