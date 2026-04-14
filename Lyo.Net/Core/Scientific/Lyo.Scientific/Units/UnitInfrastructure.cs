using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Units;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuantityDimension(
    int MassExponent,
    int LengthExponent,
    int TimeExponent,
    int CurrentExponent,
    int TemperatureExponent,
    int AmountExponent,
    int LuminousIntensityExponent)
{
    public static readonly QuantityDimension Dimensionless = new(0, 0, 0, 0, 0, 0, 0);

    public override string ToString()
        => $"[M{MassExponent} L{LengthExponent} T{TimeExponent} I{CurrentExponent} Θ{TemperatureExponent} N{AmountExponent} J{LuminousIntensityExponent}]";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DerivedUnitDefinition(string Name, string Symbol, QuantityDimension Dimension, double ToSiFactor)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public string Symbol { get; init; } = string.IsNullOrWhiteSpace(Symbol) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Symbol)) : Symbol;

    public double ToSiFactor { get; init; } = ToSiFactor <= 0d ? throw new ArgumentOutOfRangeException(nameof(ToSiFactor)) : ToSiFactor;

    public override string ToString() => $"{Symbol} ({Name}), SI×{ToSiFactor}, {Dimension}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DimensionedValue(double ValueSi, QuantityDimension Dimension)
{
    public double ValueSi { get; } = double.IsNaN(ValueSi) || double.IsInfinity(ValueSi) ? throw new ArgumentOutOfRangeException(nameof(ValueSi)) : ValueSi;

    public QuantityDimension Dimension { get; } = Dimension;

    public override string ToString() => $"{ValueSi} (SI), {Dimension}";
}

public static class DerivedUnits
{
    public static IReadOnlyDictionary<string, DerivedUnitDefinition> BySymbol { get; } = new Dictionary<string, DerivedUnitDefinition>(StringComparer.OrdinalIgnoreCase) {
        ["N"] = new("Newton", "N", new(1, 1, -2, 0, 0, 0, 0), 1d),
        ["J"] = new("Joule", "J", new(1, 2, -2, 0, 0, 0, 0), 1d),
        ["W"] = new("Watt", "W", new(1, 2, -3, 0, 0, 0, 0), 1d),
        ["Pa"] = new("Pascal", "Pa", new(1, -1, -2, 0, 0, 0, 0), 1d),
        ["Hz"] = new("Hertz", "Hz", new(0, 0, -1, 0, 0, 0, 0), 1d),
        ["C"] = new("Coulomb", "C", new(0, 0, 1, 1, 0, 0, 0), 1d),
        ["V"] = new("Volt", "V", new(1, 2, -3, -1, 0, 0, 0), 1d),
        ["ohm"] = new("Ohm", "ohm", new(1, 2, -3, -2, 0, 0, 0), 1d)
    };
}

public static class UnitConversion
{
    public static double Convert(double value, DerivedUnitDefinition fromUnit, DerivedUnitDefinition toUnit)
    {
        EnsureCompatible(fromUnit.Dimension, toUnit.Dimension);
        return value * fromUnit.ToSiFactor / toUnit.ToSiFactor;
    }

    public static double ApplyPrefix(double value, ScientificUnitPrefix prefix) => value * prefix.Multiplier;

    public static void EnsureCompatible(QuantityDimension left, QuantityDimension right) => OperationHelpers.ThrowIf(left != right, "Unit dimensions are not compatible.");

    public static DimensionedValue Add(DimensionedValue left, DimensionedValue right)
    {
        EnsureCompatible(left.Dimension, right.Dimension);
        return new(left.ValueSi + right.ValueSi, left.Dimension);
    }
}