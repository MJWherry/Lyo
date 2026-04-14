using Lyo.Exceptions;
using Lyo.Scientific.Astronomy;
using Lyo.Scientific.Chemistry;
using Lyo.Scientific.Engineering;

namespace Lyo.Scientific;

public static class ScientificWorkflowExtensions
{
    public static double MolarMassEstimate(this ChemicalCompound compound)
    {
        ArgumentHelpers.ThrowIfNull(compound, nameof(compound));
        return compound.Parts.Sum(part => ElementAtomicMasses.BySymbol[part.Element.Symbol] * part.Count);
    }

    public static double InAstronomicalUnits(this PlanetaryBody body)
    {
        ArgumentHelpers.ThrowIfNull(body, nameof(body));
        return body.SemiMajorAxis.Meters / AstronomyReferenceValues.AstronomicalUnit.Meters;
    }

    public static MaterialProperty GetMaterial(this string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return EngineeringMaterials.Common.First(material => string.Equals(material.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}