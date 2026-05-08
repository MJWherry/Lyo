using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Angular momentum stored in kilogram square meters per second.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularMomentum
{
    /// <summary>Same quantity expressed in KilogramSquareMetersPerSecond.</summary>
    public double KilogramSquareMetersPerSecond { get; }

    public AngularMomentum(double kilogramSquareMetersPerSecond)
        => KilogramSquareMetersPerSecond = MathValueGuards.Finite(kilogramSquareMetersPerSecond, nameof(kilogramSquareMetersPerSecond));

    public static AngularMomentum FromKilogramSquareMetersPerSecond(double kilogramSquareMetersPerSecond) => new(kilogramSquareMetersPerSecond);

    public override string ToString() => $"{KilogramSquareMetersPerSecond:0.###} kg*m^2/s";
}