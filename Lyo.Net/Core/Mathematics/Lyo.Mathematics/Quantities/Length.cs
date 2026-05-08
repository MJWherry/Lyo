using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed length for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Length
{
    /// <summary>Canonical SI scalar in Meters (storage for this Length).</summary>
    public double Meters { get; }

    /// <summary>Same quantity expressed in Centimeters.</summary>
    public double Centimeters => Meters * 100d;

    /// <summary>Same quantity expressed in Kilometers.</summary>
    public double Kilometers => Meters / 1000d;

    /// <summary>Same quantity expressed in Inches.</summary>
    public double Inches => Meters / 0.0254d;

    /// <summary>Same quantity expressed in Feet.</summary>
    public double Feet => Meters / 0.3048d;

    /// <summary>Same quantity expressed in Miles.</summary>
    public double Miles => Meters / 1609.344d;

    public Length(double meters) => Meters = MathValueGuards.NonNegativeFinite(meters, nameof(meters));

    public static Length FromMeters(double meters) => new(meters);

    public static Length FromCentimeters(double centimeters) => new(MathValueGuards.NonNegativeFinite(centimeters, nameof(centimeters)) / 100d);

    public static Length FromKilometers(double kilometers) => new(MathValueGuards.NonNegativeFinite(kilometers, nameof(kilometers)) * 1000d);

    public static Length FromInches(double inches) => new(MathValueGuards.NonNegativeFinite(inches, nameof(inches)) * 0.0254d);

    public static Length FromFeet(double feet) => new(MathValueGuards.NonNegativeFinite(feet, nameof(feet)) * 0.3048d);

    public static Length FromMiles(double miles) => new(MathValueGuards.NonNegativeFinite(miles, nameof(miles)) * 1609.344d);

    public static Length operator +(Length left, Length right) => new(left.Meters + right.Meters);

    public static Length operator -(Length left, Length right) => new(Math.Max(0d, left.Meters - right.Meters));

    public override string ToString() => $"{Meters:0.###} m";
}