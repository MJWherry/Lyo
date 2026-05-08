using Lyo.Mathematics.Registry;

namespace Lyo.Scientific;

/// <summary>Curated catalog of scientific formulas implemented in <c>Lyo.Scientific.Functions</c>.</summary>
/// <remarks>
/// <see cref="All" /> is intentionally short: use it for UI discovery, capability flags, or onboarding — not as a complete index of every function. Entries reuse
/// <see cref="FormulaDescriptor" /> from <c>Lyo.Mathematics</c> so scientific and mathematics registries share the same metadata shape.
/// </remarks>
public static class ScientificFormulaRegistry
{
    /// <summary>Stable ids, display names, and signature hints for representative scientific calculations.</summary>
    public static IReadOnlyList<FormulaDescriptor> All { get; } = [
        new(
            "chemistry.molar_mass", "Chemistry", "Lyo.Scientific.Functions", "Molar Mass", "Parses a compound formula and sums elemental masses.",
            "ChemistryFunctions.MolarMass(string)"),
        new(
            "chemistry.balance_reaction", "Chemistry", "Lyo.Scientific.Functions", "Balance Reaction", "Balances reactants and products into integer coefficients.",
            "ChemistryFunctions.BalanceReaction(string[], string[])"),
        new(
            "astronomy.kepler.period", "Astronomy", "Lyo.Scientific.Functions", "Orbital Period", "Computes orbital period from central mass and semi-major axis.",
            "AstronomyFunctions.OrbitalPeriod(Mass, Length)"),
        new(
            "fluid.oblique_shock", "Fluid Dynamics", "Lyo.Scientific.Functions", "Oblique Shock", "Returns the normal components and deflection for supersonic shocks.",
            "CompressibleFlowFunctions.ObliqueShock(ObliqueShockInput)"),
        new(
            "solid.fatigue_life", "Solid Mechanics", "Lyo.Scientific.Functions", "Fatigue Life", "Estimates cycles to failure from an S-N curve input.",
            "SolidMechanicsFunctions.FatigueLifeCycles(SNCurveInput)")
    ];
}