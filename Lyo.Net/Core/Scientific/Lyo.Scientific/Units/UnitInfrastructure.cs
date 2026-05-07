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
public sealed record DerivedUnitDefinition
{

    public DerivedUnitDefinition(string name, string symbol, QuantityDimension dimension, double toSiFactor)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;
        symbol = string.IsNullOrWhiteSpace(symbol) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(symbol)) : symbol;
        toSiFactor = toSiFactor <= 0d ? throw new ArgumentOutOfRangeException(nameof(toSiFactor)) : toSiFactor;

        Name = name;
        Symbol = symbol;
        ToSiFactor = toSiFactor;
        Dimension = dimension;
}


    public string Name { get;  init; }
    public string Symbol { get;  init; }
    public double ToSiFactor { get;  init; }

    public QuantityDimension Dimension { get; init; }
    public override string ToString() => $"{Symbol} ({Name}), SI×{ToSiFactor}, {Dimension}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DimensionedValue
{

    public DimensionedValue(double valueSi, QuantityDimension dimension)

    {

        valueSi = double.IsNaN(valueSi) || double.IsInfinity(valueSi) ? throw new ArgumentOutOfRangeException(nameof(valueSi)) : valueSi;
        ValueSi = valueSi;
        Dimension = dimension;

    }


    public double ValueSi { get;  }
    public QuantityDimension Dimension { get; }

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