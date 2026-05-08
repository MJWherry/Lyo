using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Plane angle stored in radians.</summary>
/// <remarks>Any finite radian measure is allowed (may represent rotations beyond one full turn).</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Angle
{
    /// <summary>Canonical SI scalar in Radians (storage for this Angle).</summary>
    public double Radians { get; }

    /// <summary>Same quantity expressed in Degrees.</summary>
    public double Degrees => Radians * (180d / Math.PI);

    public Angle(double radians) => Radians = MathValueGuards.Finite(radians, nameof(radians));

    public static Angle FromRadians(double radians) => new(radians);

    public static Angle FromDegrees(double degrees) => new(MathValueGuards.Finite(degrees, nameof(degrees)) * (Math.PI / 180d));

    public override string ToString() => $"{Degrees:0.###} deg";
}