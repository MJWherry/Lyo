using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Length(double meters)
{
    public double Meters { get; } = MathValueGuards.NonNegativeFinite(meters, nameof(meters));

    public double Centimeters => Meters * 100d;

    public double Kilometers => Meters / 1000d;

    public double Inches => Meters / 0.0254d;

    public double Feet => Meters / 0.3048d;

    public double Miles => Meters / 1609.344d;

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