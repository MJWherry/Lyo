using Lyo.Exceptions;
using Lyo.Scientific.Astronomy;
using Lyo.Scientific.Chemistry;
using Lyo.Scientific.Engineering;

namespace Lyo.Scientific;

/// <summary>Small extension methods that glue scientific reference data to quick estimates.</summary>
/// <remarks>
/// These helpers favor convenience over modeling depth. For reaction balancing, orbital mechanics, shocks, or fatigue, call the corresponding
/// <c>Lyo.Scientific.Functions.*Functions</c> static APIs instead.
/// </remarks>
public static class ScientificWorkflowExtensions
{
    /// <summary>Approximates formula mass in g/mol by summing <see cref="ElementAtomicMasses.BySymbol" /> weights for each <see cref="ChemicalCompound.Parts" /> entry.</summary>
    /// <param name="compound">Parsed compound whose element symbols exist in <see cref="ElementAtomicMasses.BySymbol" />.</param>
    /// <returns>Estimated relative molecular mass (numeric).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compound" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">An element symbol in the compound is missing from the atomic-mass table.</exception>
    public static double MolarMassEstimate(this ChemicalCompound compound)
    {
        ArgumentHelpers.ThrowIfNull(compound);
        return compound.Parts.Sum(part => ElementAtomicMasses.BySymbol[part.Element.Symbol] * part.Count);
    }

    /// <summary>Expresses the body’s semi-major axis in astronomical units using <see cref="AstronomyReferenceValues.AstronomicalUnit" />.</summary>
    /// <param name="body">Planet or dwarf planet with a populated <see cref="PlanetaryBody.SemiMajorAxis" />.</param>
    /// <returns>Semi-major axis in AU (heliocentric distance scale).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body" /> is <see langword="null" />.</exception>
    public static double InAstronomicalUnits(this PlanetaryBody body)
    {
        ArgumentHelpers.ThrowIfNull(body);
        return body.SemiMajorAxis.Meters / AstronomyReferenceValues.AstronomicalUnit.Meters;
    }

    /// <summary>Looks up a <see cref="MaterialProperty" /> from <see cref="EngineeringMaterials.Common" /> by exact name (case-insensitive).</summary>
    /// <param name="name">Material name such as <c>Steel</c> or <c>Water</c>.</param>
    /// <returns>The matching catalog entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No material matches <paramref name="name" />.</exception>
    public static MaterialProperty GetMaterial(this string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return EngineeringMaterials.Common.First(material => string.Equals(material.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}