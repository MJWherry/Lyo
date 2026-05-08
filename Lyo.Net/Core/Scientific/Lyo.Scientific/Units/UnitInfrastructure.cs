using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Units;

/// <summary>Exponent vector for the seven SI base dimensions (mass, length, time, current, thermodynamic temperature, amount of substance, luminous intensity).</summary>
/// <remarks>Used by <see cref="DerivedUnitDefinition" /> and <see cref="UnitConversion.EnsureCompatible" /> to prevent mixing incompatible quantities.</remarks>
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
    /// <summary>Dimensionless quantity (all exponents zero).</summary>
    public static readonly QuantityDimension Dimensionless = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>Returns a compact bracketed summary of dimensional exponents.</summary>
    public override string ToString()
        => $"[M{MassExponent} L{LengthExponent} T{TimeExponent} I{CurrentExponent} Θ{TemperatureExponent} N{AmountExponent} J{LuminousIntensityExponent}]";
}

/// <summary>Metadata for a named derived unit: human-readable labels, dimensional signature, and multiplicative factor to the SI canonical scalar.</summary>
/// <remarks><see cref="ToSiFactor" /> converts from this unit to SI by multiplication (<c>si = value * ToSiFactor</c>).</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DerivedUnitDefinition
{
    /// <summary>Long display name (for example <c>Newton</c>).</summary>
    public string Name { get; init; }

    /// <summary>Unit symbol (for example <c>N</c>).</summary>
    public string Symbol { get; init; }

    /// <summary>Multiplier that converts a numeric value expressed in this unit into its SI canonical form.</summary>
    public double ToSiFactor { get; init; }

    /// <summary>Dimensional exponents for this unit.</summary>
    public QuantityDimension Dimension { get; init; }

    /// <summary>Creates a derived unit definition after validating non-empty strings and a positive conversion factor.</summary>
    /// <param name="name">Human-readable unit name.</param>
    /// <param name="symbol">Abbreviated symbol.</param>
    /// <param name="dimension">Dimensional signature.</param>
    /// <param name="toSiFactor">Strictly positive factor to SI.</param>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="symbol" /> is null/whitespace.</exception>
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

/// <summary>A finite numeric magnitude stored in SI together with its dimensional signature.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DimensionedValue
{
    /// <summary>Scalar magnitude expressed in SI canonical units for the given <see cref="Dimension" />.</summary>
    public double ValueSi { get; }

    /// <summary>Dimensional exponents describing what physical quantity <see cref="ValueSi" /> represents.</summary>
    public QuantityDimension Dimension { get; }

    /// <summary>Creates a dimensioned SI value after validating finiteness.</summary>
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

/// <summary>Static catalog of common derived SI units keyed by symbol (case-insensitive).</summary>
public static class DerivedUnits
{
    /// <summary>Map from unit symbol (for example <c>Pa</c>, <c>N</c>) to <see cref="DerivedUnitDefinition" /> metadata.</summary>
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

/// <summary>Dimensionally safe conversions between derived units and light algebra on <see cref="DimensionedValue" />.</summary>
public static class UnitConversion
{
    /// <summary>Converts <paramref name="value" /> expressed in <paramref name="fromUnit" /> into <paramref name="toUnit" />.</summary>
    /// <param name="value">Magnitude in <paramref name="fromUnit" />.</param>
    /// <param name="fromUnit">Source unit definition.</param>
    /// <param name="toUnit">Target unit definition (must match dimensions).</param>
    /// <returns>Magnitude expressed in <paramref name="toUnit" />.</returns>
    public static double Convert(double value, DerivedUnitDefinition fromUnit, DerivedUnitDefinition toUnit)
    {
        EnsureCompatible(fromUnit.Dimension, toUnit.Dimension);
        return value * fromUnit.ToSiFactor / toUnit.ToSiFactor;
    }

    /// <summary>Multiplies <paramref name="value" /> by the metric prefix <paramref name="prefix" />.</summary>
    public static double ApplyPrefix(double value, ScientificUnitPrefix prefix) => value * prefix.Multiplier;

    /// <summary>Ensures two dimensional signatures match; otherwise throws via <see cref="OperationHelpers" />.</summary>
    public static void EnsureCompatible(QuantityDimension left, QuantityDimension right) => OperationHelpers.ThrowIf(left != right, "Unit dimensions are not compatible.");

    /// <summary>Adds two SI magnitudes after verifying compatible dimensions.</summary>
    public static DimensionedValue Add(DimensionedValue left, DimensionedValue right)
    {
        EnsureCompatible(left.Dimension, right.Dimension);
        return new(left.ValueSi + right.ValueSi, left.Dimension);
    }
}