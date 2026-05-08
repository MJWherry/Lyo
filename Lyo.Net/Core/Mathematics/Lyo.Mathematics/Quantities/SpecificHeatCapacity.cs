using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed specific \1eat \1apacity for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpecificHeatCapacity
{
    /// <summary>Same quantity expressed in JoulesPerKilogramKelvin.</summary>
    public double JoulesPerKilogramKelvin { get; }

    public SpecificHeatCapacity(double joulesPerKilogramKelvin)
        => JoulesPerKilogramKelvin = MathValueGuards.NonNegativeFinite(joulesPerKilogramKelvin, nameof(joulesPerKilogramKelvin));

    public static SpecificHeatCapacity FromJoulesPerKilogramKelvin(double joulesPerKilogramKelvin) => new(joulesPerKilogramKelvin);

    public override string ToString() => $"{JoulesPerKilogramKelvin:0.###} J/(kg*K)";
}