using Lyo.Mathematics.Registry;

namespace Lyo.Scientific;

public static class ScientificFormulaRegistry
{
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