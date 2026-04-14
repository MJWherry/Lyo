using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Temperature(double kelvin)
{
    public double Kelvin { get; } = MathValueGuards.PositiveFinite(kelvin, nameof(kelvin));

    public double Celsius => Kelvin - 273.15d;

    public double Fahrenheit => Celsius * 9d / 5d + 32d;

    public static Temperature FromKelvin(double kelvin) => new(kelvin);

    public static Temperature FromCelsius(double celsius) => new(celsius + 273.15d);

    public static Temperature FromFahrenheit(double fahrenheit) => FromCelsius((fahrenheit - 32d) * 5d / 9d);

    public override string ToString() => $"{Kelvin:0.###} K";
}