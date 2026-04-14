using Lyo.Mathematics.Quantities;
using Lyo.Scientific.Astronomy;
using Lyo.Scientific.Chemistry;
using Lyo.Scientific.Functions;
using Lyo.Scientific.Units;

namespace Lyo.Scientific.Tests;

public class ScientificLibraryTests
{
    [Fact]
    public void ScientificConstants_ExposeStableValues()
    {
        Assert.Equal(Math.PI, ScientificConstants.Pi, 10);
        Assert.True(ScientificConstants.GasConstant > 8d);
        Assert.True(ScientificConstants.StandardGravity > 9d);
    }

    [Fact]
    public void PeriodicTable_ContainsExpectedElementCount() => Assert.Equal(118, PeriodicTable.All.Count);

    [Fact]
    public void ChemistryFunctions_GetElementByAtomicNumber_ReturnsExpectedElement()
    {
        var result = ChemistryFunctions.GetElementByAtomicNumber(8);
        Assert.Equal("O", result.Symbol);
        Assert.Equal("Oxygen", result.Name);
    }

    [Fact]
    public void ChemistryFunctions_GetElementBySymbol_IsCaseInsensitive()
    {
        var result = ChemistryFunctions.GetElementBySymbol("au");
        Assert.Equal(79, result.AtomicNumber);
        Assert.Equal("Gold", result.Name);
    }

    [Fact]
    public void PlanetaryBodies_ContainsExpectedEarthRecord()
    {
        var earth = PlanetaryBodies.All.Single(body => body.Name == "Earth");
        Assert.Equal(PlanetaryBodyKind.Planet, earth.Kind);
        Assert.Equal(1, earth.NaturalSatelliteCount);
    }

    [Fact]
    public void AstronomyFunctions_SurfaceGravity_ComputesEarthLikeValue()
    {
        var earth = AstronomyFunctions.GetPlanetaryBodyByName("earth");
        var result = AstronomyFunctions.SurfaceGravity(earth);
        Assert.Equal(9.8d, result.MetersPerSecondSquared, 1);
    }

    [Fact]
    public void AstronomyFunctions_EscapeVelocity_ComputesEarthLikeValue()
    {
        var earth = AstronomyFunctions.GetPlanetaryBodyByName("Earth");
        var result = AstronomyFunctions.EscapeVelocity(earth);
        Assert.InRange(result.MetersPerSecond, 11_000d, 11_300d);
    }

    [Fact]
    public void AstronomyFunctions_OrbitalCircumference_UsesSemiMajorAxis()
    {
        var earth = AstronomyFunctions.GetPlanetaryBodyByName("Earth");
        var result = AstronomyFunctions.OrbitalCircumference(earth);
        Assert.True(result.Meters > 900_000_000_000d);
    }

    [Fact]
    public void ChemistryFunctions_ParseFormula_ComputesExpectedParts()
    {
        var result = ChemistryFunctions.ParseFormula("H2O");
        Assert.Equal(2, result.Parts.Count);
        Assert.Equal("H", result.Parts[0].Element.Symbol);
        Assert.Equal(2, result.Parts[0].Count);
        Assert.Equal("O", result.Parts[1].Element.Symbol);
    }

    [Fact]
    public void ChemistryFunctions_MolarMass_ComputesExpectedValue()
    {
        var result = ChemistryFunctions.MolarMass("H2O");
        Assert.Equal(18.015d, result, 3);
    }

    [Fact]
    public void ChemistryFunctions_ParseFormula_SupportsParentheses()
    {
        var result = ChemistryFunctions.ParseFormula("Ca(OH)2");
        Assert.Equal(3, result.Parts.Count);
        Assert.Equal(2, result.Parts.Single(part => part.Element.Symbol == "O").Count);
        Assert.Equal(2, result.Parts.Single(part => part.Element.Symbol == "H").Count);
    }

    [Fact]
    public void ChemistryFunctions_BalanceReaction_ComputesExpectedCoefficients()
    {
        var result = ChemistryFunctions.BalanceReaction(["CH4", "O2"], ["CO2", "H2O"]);
        Assert.Equal(1, result.Reactants.Single(item => item.Formula == "CH4").Coefficient);
        Assert.Equal(2, result.Reactants.Single(item => item.Formula == "O2").Coefficient);
        Assert.Equal(1, result.Products.Single(item => item.Formula == "CO2").Coefficient);
        Assert.Equal(2, result.Products.Single(item => item.Formula == "H2O").Coefficient);
    }

    [Fact]
    public void ChemistryFunctions_StoichiometricProductMass_ComputesExpectedWaterMass()
    {
        var balanced = ChemistryFunctions.BalanceReaction(["H2", "O2"], ["H2O"]);
        var result = ChemistryFunctions.StoichiometricProductMass("H2", 4.032d, "H2O", balanced);
        Assert.InRange(result.ProductMassGrams, 35.9d, 36.1d);
    }

    [Fact]
    public void ChemistryFunctions_AllIsotopes_ExposeCommonRecords()
        => Assert.Contains(ChemistryFunctions.AllIsotopes(), isotope => isotope.Symbol == "C" && isotope.MassNumber == 13);

    [Fact]
    public void AstronomyFunctions_OrbitalVelocity_ComputesEarthLikeValue()
    {
        var result = AstronomyFunctions.OrbitalVelocity(AstronomyReferenceValues.SolarMass, AstronomyReferenceValues.AstronomicalUnit);
        Assert.InRange(result.MetersPerSecond, 29_000d, 31_000d);
    }

    [Fact]
    public void AstronomyFunctions_EquilibriumTemperature_ComputesEarthLikeBand()
    {
        var sun = AstronomyFunctions.GetStarByName("Sun");
        var result = AstronomyFunctions.EquilibriumTemperature(sun.Luminosity, AstronomyReferenceValues.AstronomicalUnit, 0.3d);
        Assert.InRange(result.Kelvin, 240d, 270d);
    }

    [Fact]
    public void AstronomyFunctions_SemiMajorAxisFromPeriod_ComputesAstronomicalUnitLikeValue()
    {
        var result = AstronomyFunctions.SemiMajorAxisFromPeriod(AstronomyReferenceValues.SolarMass, TimeInterval.FromSeconds(31_557_600d));
        Assert.InRange(result.Meters, 1.45e11d, 1.55e11d);
    }

    [Fact]
    public void AstronomyFunctions_GetExoplanetByName_ReturnsExpectedRecord()
    {
        var result = AstronomyFunctions.GetExoplanetByName("Proxima Centauri b");
        Assert.True(result.PotentiallyHabitable);
    }

    [Fact]
    public void AstronomyFunctions_MagnitudeDifferenceFromLuminosityRatio_ComputesExpectedValue()
    {
        var result = AstronomyFunctions.MagnitudeDifferenceFromLuminosityRatio(100d);
        Assert.Equal(-5d, result, 10);
    }

    [Fact]
    public void ScientificUnitPrefixes_ContainMetricPrefix() => Assert.Contains(ScientificUnitPrefixes.Metric, prefix => prefix.Symbol == "k" && prefix.Multiplier == 1e3);

    [Fact]
    public void UnitConversion_Convert_ComputesEquivalentPressureUnits()
    {
        var result = UnitConversion.Convert(1d, DerivedUnits.BySymbol["Pa"], DerivedUnits.BySymbol["Pa"]);
        Assert.Equal(1d, result, 10);
    }

    [Fact]
    public void ScientificFormulaRegistry_ExposesProductionEntries()
    {
        Assert.Contains(ScientificFormulaRegistry.All, item => item.Id == "chemistry.balance_reaction");
        Assert.Contains(ScientificFormulaRegistry.All, item => item.Id == "solid.fatigue_life");
    }
}