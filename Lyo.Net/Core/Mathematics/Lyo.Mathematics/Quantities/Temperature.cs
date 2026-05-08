using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Absolute temperature stored in kelvin.</summary>
/// <remarks>Kelvin must be strictly positive (above absolute zero). Celsius and Fahrenheit are derived for display.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Temperature
{
    /// <summary>Canonical SI scalar in Kelvin (storage for this Temperature).</summary>
    public double Kelvin { get; }

    /// <summary>Same quantity expressed in Celsius.</summary>
    public double Celsius => Kelvin - 273.15d;

    /// <summary>Same quantity expressed in Fahrenheit.</summary>
    public double Fahrenheit => Celsius * 9d / 5d + 32d;

    public Temperature(double kelvin) => Kelvin = MathValueGuards.PositiveFinite(kelvin, nameof(kelvin));

    public static Temperature FromKelvin(double kelvin) => new(kelvin);

    public static Temperature FromCelsius(double celsius) => new(celsius + 273.15d);

    public static Temperature FromFahrenheit(double fahrenheit) => FromCelsius((fahrenheit - 32d) * 5d / 9d);

    public override string ToString() => $"{Kelvin:0.###} K";
}