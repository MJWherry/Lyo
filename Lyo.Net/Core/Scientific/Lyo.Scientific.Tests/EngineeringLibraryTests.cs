using Lyo.Mathematics.Quantities;
using Lyo.Scientific.Engineering;
using Lyo.Scientific.Functions;

namespace Lyo.Scientific.Tests;

public class EngineeringLibraryTests
{
    [Fact]
    public void EngineeringMaterials_ExposeCommonMaterials()
    {
        Assert.Contains(EngineeringMaterials.Common, material => material.Name == "Water");
        Assert.Contains(EngineeringMaterials.Common, material => material.Name == "Steel");
        Assert.Contains(EngineeringMaterials.Common, material => material.Name == "Copper");
    }

    [Fact]
    public void ThermodynamicsFunctions_HeatEnergy_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.HeatEnergy(
            new(Mass.FromKilograms(2d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(4184d), Temperature.FromCelsius(20d), Temperature.FromCelsius(30d)));

        Assert.Equal(83680d, result.Joules, 10);
    }

    [Fact]
    public void ThermodynamicsFunctions_CarnotEfficiency_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.CarnotEfficiency(Temperature.FromKelvin(600d), Temperature.FromKelvin(300d));
        Assert.Equal(0.5d, result, 10);
    }

    [Fact]
    public void ThermodynamicsFunctions_ConvectiveHeatTransferRate_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.ConvectiveHeatTransferRate(
            new(HeatTransferCoefficient.FromWattsPerSquareMeterKelvin(10d), Area.FromSquareMeters(2d), Temperature.FromCelsius(60d), Temperature.FromCelsius(20d)));

        Assert.Equal(800d, result.Watts, 10);
    }

    [Fact]
    public void ThermodynamicsFunctions_HeatExchanger_ComputesOutletTemperatures()
    {
        var result = ThermodynamicsFunctions.HeatExchanger(
            new(
                MassFlowRate.FromKilogramsPerSecond(1d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(4184d), Temperature.FromCelsius(80d),
                MassFlowRate.FromKilogramsPerSecond(1d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(4184d), Temperature.FromCelsius(20d), 0.5d));

        Assert.Equal(125520d, result.HeatTransferred.Joules, 10);
        Assert.Equal(50d, result.HotOutletTemperature.Celsius, 10);
        Assert.Equal(50d, result.ColdOutletTemperature.Celsius, 10);
    }

    [Fact]
    public void ThermodynamicsFunctions_PrandtlNumber_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.PrandtlNumber(
            SpecificHeatCapacity.FromJoulesPerKilogramKelvin(1005d), DynamicViscosity.FromPascalSeconds(1.81e-5d), ThermalConductivity.FromWattsPerMeterKelvin(0.026d));

        Assert.InRange(result, 0.69d, 0.71d);
    }

    [Fact]
    public void ThermodynamicsFunctions_HeatTransferCoefficientFromNusselt_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.HeatTransferCoefficientFromNusselt(
            new(10000d, 0.7d, Length.FromMeters(0.05d), ThermalConductivity.FromWattsPerMeterKelvin(0.026d), true));

        Assert.True(result.WattsPerSquareMeterKelvin > 10d);
    }

    [Fact]
    public void ThermodynamicsFunctions_RadiationExchangeRate_ComputesExpectedValue()
    {
        var result = ThermodynamicsFunctions.RadiationExchangeRate(new(0.9d, 0.8d, Area.FromSquareMeters(1d), Temperature.FromKelvin(500d), Temperature.FromKelvin(300d), 1d));
        Assert.True(result.Watts > 2000d);
    }

    [Fact]
    public void ThermodynamicsFunctions_SpeedOfSoundIdealGas_ComputesAirLikeValue()
    {
        var result = ThermodynamicsFunctions.SpeedOfSoundIdealGas(Temperature.FromKelvin(288.15d), 1.4d, 0.02897d);
        Assert.InRange(result.MetersPerSecond, 330d, 345d);
    }

    [Fact]
    public void FluidDynamicsFunctions_ReynoldsNumber_ComputesExpectedValue()
    {
        var result = FluidDynamicsFunctions.ReynoldsNumber(
            new(
                new(
                    Density.FromKilogramsPerCubicMeter(1000d), Velocity.FromMetersPerSecond(2d), Pressure.FromPascals(101325d), DynamicViscosity.FromPascalSeconds(0.001d),
                    Length.FromMeters(0.05d))));

        Assert.Equal(100000d, result, 10);
    }

    [Fact]
    public void FluidDynamicsFunctions_DragForce_ComputesExpectedValue()
    {
        var result = FluidDynamicsFunctions.DragForce(new(Density.FromKilogramsPerCubicMeter(1.225d), Velocity.FromMetersPerSecond(10d), 1d, Area.FromSquareMeters(1d)));
        Assert.Equal(61.25d, result.Newtons, 10);
    }

    [Fact]
    public void FluidDynamicsFunctions_BuoyantForce_ComputesExpectedValue()
    {
        var result = FluidDynamicsFunctions.BuoyantForce(
            new(Density.FromKilogramsPerCubicMeter(1000d), Volume.FromCubicMeters(0.01d), Acceleration.FromMetersPerSecondSquared(ScientificConstants.StandardGravity)));

        Assert.Equal(98.0665d, result.Newtons, 10);
    }

    [Fact]
    public void CompressibleFlowFunctions_StaticTemperature_ComputesExpectedValue()
    {
        var result = CompressibleFlowFunctions.StaticTemperature(new(Pressure.FromPascals(200000d), Temperature.FromKelvin(300d), 1.4d, 287d, 2d));
        Assert.InRange(result.Kelvin, 165d, 170d);
    }

    [Fact]
    public void CompressibleFlowFunctions_NozzleFlow_ComputesPositiveOutputs()
    {
        var result = CompressibleFlowFunctions.NozzleFlow(
            new(Pressure.FromPascals(500000d), Temperature.FromKelvin(600d), Pressure.FromPascals(101325d), 1.4d, 287d), Area.FromSquareMeters(0.01d));

        Assert.True(result.ExitVelocity.MetersPerSecond > 400d);
        Assert.NotNull(result.ChokedMassFlowRate);
        Assert.True(result.ChokedMassFlowRate!.Value.KilogramsPerSecond > 0d);
    }

    [Fact]
    public void CompressibleFlowFunctions_IsentropicAreaMachRatio_IsGreaterThanOne()
    {
        var result = CompressibleFlowFunctions.IsentropicAreaMachRatio(1.4d, 2d);
        Assert.InRange(result, 1.6d, 1.8d);
    }

    [Fact]
    public void CompressibleFlowFunctions_DownstreamMachNormalShock_ComputesSubsonicValue()
    {
        var result = CompressibleFlowFunctions.DownstreamMachNormalShock(new(2d, 1.4d));
        Assert.InRange(result, 0.57d, 0.58d);
    }

    [Fact]
    public void CompressibleFlowFunctions_SolveMachFromAreaRatio_ComputesSupersonicBranch()
    {
        var result = CompressibleFlowFunctions.SolveMachFromAreaRatio(1.6875d, 1.4d, true);
        Assert.InRange(result, 1.99d, 2.01d);
    }

    [Fact]
    public void CompressibleFlowFunctions_ObliqueShock_ComputesExpectedDeflection()
    {
        var result = CompressibleFlowFunctions.ObliqueShock(new(2d, Angle.FromDegrees(40d), 1.4d));
        Assert.InRange(result.NormalMachBefore, 1.28d, 1.29d);
        Assert.InRange(result.FlowDeflectionAngle.Degrees, 10d, 11d);
        Assert.True(result.PressureRatio > 1d);
    }

    [Fact]
    public void MechanicsFunctions_RotationalKineticEnergy_ComputesExpectedValue()
    {
        var result = MechanicsFunctions.RotationalKineticEnergy(new(MomentOfInertia.FromKilogramSquareMeters(2d), AngularVelocity.FromRadiansPerSecond(3d)));
        Assert.Equal(9d, result.Joules, 10);
    }

    [Fact]
    public void MechanicsFunctions_PendulumPeriod_ComputesExpectedValue()
    {
        var result = MechanicsFunctions.PendulumPeriod(new(Length.FromMeters(1d), Acceleration.FromMetersPerSecondSquared(ScientificConstants.StandardGravity)));
        Assert.InRange(result.Seconds, 1.9d, 2.1d);
    }

    [Fact]
    public void MechanicsFunctions_AngularMomentum_ComputesExpectedValue()
    {
        var result = MechanicsFunctions.AngularMomentum(new(MomentOfInertia.FromKilogramSquareMeters(2d), AngularVelocity.FromRadiansPerSecond(3d)));
        Assert.Equal(6d, result.KilogramSquareMetersPerSecond, 10);
    }

    [Fact]
    public void SolidMechanicsFunctions_NormalStress_ComputesExpectedValue()
    {
        var result = SolidMechanicsFunctions.NormalStress(new(Force.FromNewtons(1000d), Area.FromSquareMeters(0.01d), Length.FromMeters(2d), Length.FromMeters(0.002d)));
        Assert.Equal(100000d, result.Pascals, 10);
    }

    [Fact]
    public void SolidMechanicsFunctions_CantileverEndDeflection_ComputesExpectedValue()
    {
        var result = SolidMechanicsFunctions.CantileverEndDeflection(
            new(Force.FromNewtons(100d), Length.FromMeters(2d), ModulusOfElasticity.FromPascals(200e9d), AreaMomentOfInertia.FromMetersToFourth(1e-6d), Length.FromMeters(0.05d)));

        Assert.InRange(result.Meters, 0.001d, 0.002d);
    }

    [Fact]
    public void SolidMechanicsFunctions_CriticalFractureStress_ComputesExpectedValue()
    {
        var result = SolidMechanicsFunctions.CriticalFractureStress(new(Pressure.FromPascals(100e6d), Length.FromMeters(0.01d), FractureToughness.FromPascalRootMeters(30e6d)));
        Assert.InRange(result.Pascals, 160e6d, 180e6d);
    }

    [Fact]
    public void SolidMechanicsFunctions_RectangularAreaMomentOfInertia_ComputesExpectedValue()
    {
        var result = SolidMechanicsFunctions.RectangularAreaMomentOfInertia(new(Length.FromMeters(0.1d), Length.FromMeters(0.2d)));
        Assert.Equal(6.666666666666667e-5d, result.MetersToFourth, 12);
    }

    [Fact]
    public void SolidMechanicsFunctions_GoodmanFactorOfSafety_ComputesExpectedValue()
    {
        var result = SolidMechanicsFunctions.GoodmanFactorOfSafety(
            new(Pressure.FromPascals(100e6d), Pressure.FromPascals(50e6d), Pressure.FromPascals(200e6d), Pressure.FromPascals(400e6d)));

        Assert.Equal(1.6d, result, 10);
    }

    [Fact]
    public void BeamSectionCatalog_ContainsReusableProfiles() => Assert.Contains(BeamSectionCatalog.Common, profile => profile.Name.Contains("Rectangular"));

    [Fact]
    public void SolidMechanicsFunctions_BeamBendingStress_ProfileOverload_ComputesExpectedValue()
    {
        var profile = BeamSectionCatalog.Common[0];
        var result = SolidMechanicsFunctions.BeamBendingStress(Force.FromNewtons(100d), Length.FromMeters(2d), profile);
        Assert.True(result.Pascals > 0d);
    }

    [Fact]
    public void SolidMechanicsFunctions_FatigueLifeCycles_ComputesPositiveLife()
    {
        var result = SolidMechanicsFunctions.FatigueLifeCycles(new(Pressure.FromPascals(250e6d), 900e6d, -0.12d));
        Assert.True(result > 1_000d);
    }
}