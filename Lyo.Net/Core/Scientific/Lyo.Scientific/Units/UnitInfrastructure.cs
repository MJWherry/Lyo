using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Units;

/// <summary>Exponent vector for the seven SI base dimensions: mass, length, time, current, thermodynamic temperature, amount of substance, and luminous intensity.</summary>
/// <remarks><see cref="DerivedUnitDefinition" /> and <see cref="UnitConversion.EnsureCompatible" /> use this to reject mixed incompatible quantities.</remarks>
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
    /// <summary>Dimensionless quantity; every exponent is zero.</summary>
    public static readonly QuantityDimension Dimensionless = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>Compact bracketed dump of the dimensional exponents.</summary>
    public override string ToString()
        => $"[M{MassExponent} L{LengthExponent} T{TimeExponent} I{CurrentExponent} Θ{TemperatureExponent} N{AmountExponent} J{LuminousIntensityExponent}]";
}

/// <summary>Catalog row for a named derived unit: labels, dimensional signature, and the multiplicative factor to the SI canonical scalar.</summary>
/// <remarks><see cref="ToSiFactor" /> converts from this unit to SI by multiplication (<c>si = value * ToSiFactor</c>).</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DerivedUnitDefinition
{
    /// <summary>Long display name, for example <c>Newton</c>.</summary>
    public string Name { get; init; }

    /// <summary>Unit symbol, for example <c>N</c>.</summary>
    public string Symbol { get; init; }

    /// <summary>Factor that converts a numeric value in this unit into SI canonical form.</summary>
    public double ToSiFactor { get; init; }

    /// <summary>Dimensional exponents that describe this unit.</summary>
    public QuantityDimension Dimension { get; init; }

    /// <summary>Mints a derived-unit definition after checking non-empty strings and a positive conversion factor.</summary>
    /// <param name="name">Human-readable unit name.</param>
    /// <param name="symbol">Abbreviated symbol.</param>
    /// <param name="dimension">Dimensional signature.</param>
    /// <param name="toSiFactor">Strictly positive factor to SI.</param>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="symbol" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="toSiFactor" /> is not strictly positive.</exception>
    public DerivedUnitDefinition(string name, string symbol, QuantityDimension dimension, double toSiFactor)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentHelpers.ThrowIfLessThanOrEqual(toSiFactor, 0d);
        Name = name;
        Symbol = symbol;
        ToSiFactor = toSiFactor;
        Dimension = dimension;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Symbol} ({Name}), SI×{ToSiFactor}, {Dimension}";
}

/// <summary>Finite numeric magnitude stored in SI together with its dimensional signature.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DimensionedValue
{
    /// <summary>Scalar magnitude in SI canonical units for <see cref="Dimension" />.</summary>
    public double ValueSi { get; }

    /// <summary>Dimensional exponents that describe the physical quantity behind <see cref="ValueSi" />.</summary>
    public QuantityDimension Dimension { get; }

    /// <summary>Mints a dimensioned SI value after checking that the magnitude is finite.</summary>
    /// <param name="valueSi">Finite SI magnitude.</param>
    /// <param name="dimension">Dimensional signature.</param>
    /// <exception cref="ArgumentException"><paramref name="valueSi" /> is NaN or infinite.</exception>
    public DimensionedValue(double valueSi, QuantityDimension dimension)
    {
        ArgumentHelpers.ThrowIf(double.IsNaN(valueSi) || double.IsInfinity(valueSi), "Value must be a finite number.", nameof(valueSi));
        ValueSi = valueSi;
        Dimension = dimension;
    }

    /// <inheritdoc />
    public override string ToString() => $"{ValueSi} (SI), {Dimension}";
}

/// <summary>Catalog of common derived SI units keyed by symbol (case-insensitive).</summary>
public static class DerivedUnits
{
    /// <summary>Lookup from unit symbol (for example <c>Pa</c>, <c>N</c>) to <see cref="DerivedUnitDefinition" /> metadata.</summary>
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

/// <summary>Dimensionally safe conversions between derived units, plus light algebra on <see cref="DimensionedValue" />.</summary>
public static class UnitConversion
{
    /// <summary>Converts <paramref name="value" /> from <paramref name="fromUnit" /> into <paramref name="toUnit" />.</summary>
    /// <param name="value">Magnitude in <paramref name="fromUnit" />.</param>
    /// <param name="fromUnit">Source unit definition.</param>
    /// <param name="toUnit">Target unit definition (dimensions must match).</param>
    /// <returns>Magnitude expressed in <paramref name="toUnit" />.</returns>
    public static double Convert(double value, DerivedUnitDefinition fromUnit, DerivedUnitDefinition toUnit)
    {
        EnsureCompatible(fromUnit.Dimension, toUnit.Dimension);
        return value * fromUnit.ToSiFactor / toUnit.ToSiFactor;
    }

    /// <summary>Scales <paramref name="value" /> by the metric prefix <paramref name="prefix" />.</summary>
    public static double ApplyPrefix(double value, ScientificUnitPrefix prefix) => value * prefix.Multiplier;

    /// <summary>Requires two dimensional signatures to match; otherwise throws via <see cref="OperationHelpers" />.</summary>
    public static void EnsureCompatible(QuantityDimension left, QuantityDimension right) => OperationHelpers.ThrowIf(left != right, "Unit dimensions are not compatible.");

    /// <summary>Adds two SI magnitudes after checking that their dimensions match.</summary>
    public static DimensionedValue Add(DimensionedValue left, DimensionedValue right)
    {
        EnsureCompatible(left.Dimension, right.Dimension);
        return new(left.ValueSi + right.ValueSi, left.Dimension);
    }
}