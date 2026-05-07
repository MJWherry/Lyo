using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Velocity
{
    public double MetersPerSecond { get; }

    public double KilometersPerHour => MetersPerSecond * 3.6d;

    public double MilesPerHour => MetersPerSecond * 2.2369362920544d;

    public Velocity(double metersPerSecond) => MetersPerSecond = MathValueGuards.Finite(metersPerSecond, nameof(metersPerSecond));

    public static Velocity FromMetersPerSecond(double metersPerSecond) => new(metersPerSecond);

    public static Velocity FromKilometersPerHour(double kilometersPerHour) => new(MathValueGuards.Finite(kilometersPerHour, nameof(kilometersPerHour)) / 3.6d);

    public static Velocity FromMilesPerHour(double milesPerHour) => new(MathValueGuards.Finite(milesPerHour, nameof(milesPerHour)) / 2.2369362920544d);

    public override string ToString() => $"{MetersPerSecond:0.###} m/s";
}