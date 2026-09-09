using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed velocity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Velocity
{
    /// <summary>This quantity in MetersPerSecond.</summary>
    public double MetersPerSecond { get; }

    /// <summary>This quantity in KilometersPerHour.</summary>
    public double KilometersPerHour => MetersPerSecond * 3.6d;

    /// <summary>This quantity in MilesPerHour.</summary>
    public double MilesPerHour => MetersPerSecond * 2.2369362920544d;

    public Velocity(double metersPerSecond) => MetersPerSecond = MathValueGuards.Finite(metersPerSecond, nameof(metersPerSecond));

    public static Velocity FromMetersPerSecond(double metersPerSecond) => new(metersPerSecond);

    public static Velocity FromKilometersPerHour(double kilometersPerHour) => new(MathValueGuards.Finite(kilometersPerHour, nameof(kilometersPerHour)) / 3.6d);

    public static Velocity FromMilesPerHour(double milesPerHour) => new(MathValueGuards.Finite(milesPerHour, nameof(milesPerHour)) / 2.2369362920544d);

    public override string ToString() => $"{MetersPerSecond:0.###} m/s";
}