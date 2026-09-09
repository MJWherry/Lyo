using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed specific \1eat \1apacity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpecificHeatCapacity
{
    /// <summary>This quantity in JoulesPerKilogramKelvin.</summary>
    public double JoulesPerKilogramKelvin { get; }

    public SpecificHeatCapacity(double joulesPerKilogramKelvin)
        => JoulesPerKilogramKelvin = MathValueGuards.NonNegativeFinite(joulesPerKilogramKelvin, nameof(joulesPerKilogramKelvin));

    public static SpecificHeatCapacity FromJoulesPerKilogramKelvin(double joulesPerKilogramKelvin) => new(joulesPerKilogramKelvin);

    public override string ToString() => $"{JoulesPerKilogramKelvin:0.###} J/(kg*K)";
}