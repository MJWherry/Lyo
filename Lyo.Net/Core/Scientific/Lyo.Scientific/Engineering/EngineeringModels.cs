using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Engineering;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MaterialProperty(
    string Name,
    Density Density,
    SpecificHeatCapacity SpecificHeatCapacity,
    ThermalConductivity ThermalConductivity,
    DynamicViscosity? DynamicViscosity = null,
    ThermalExpansionCoefficient? ThermalExpansionCoefficient = null,
    ModulusOfElasticity? ModulusOfElasticity = null,
    Pressure? YieldStrength = null,
    FractureToughness? FractureToughness = null)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, ρ={Density}, cp={SpecificHeatCapacity}, k={ThermalConductivity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermodynamicState(Temperature Temperature, Pressure Pressure, Volume Volume, Mass? Mass = null, double? Moles = null)
{
    public double? Moles { get; } = Moles is not null && Moles < 0d ? throw new ArgumentOutOfRangeException(nameof(Moles)) : Moles;

    public override string ToString()
        => $"T={Temperature}, P={Pressure}, V={Volume}, m={ScientificModelDisplay.NullProp(Mass, static m => m.ToString())}, n={ScientificModelDisplay.NullProp(Moles, static n => n.ToString())}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatTransferInput(Mass Mass, SpecificHeatCapacity SpecificHeatCapacity, Temperature InitialTemperature, Temperature FinalTemperature)
{
    public override string ToString() => $"m={Mass}, cp={SpecificHeatCapacity}, {InitialTemperature}→{FinalTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConductionInput(ThermalConductivity ThermalConductivity, Area Area, Length Thickness, Temperature HotTemperature, Temperature ColdTemperature)
{
    public override string ToString() => $"k={ThermalConductivity}, A={Area}, t={Thickness}, Th={HotTemperature}, Tc={ColdTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalExpansionInput(Length InitialLength, ThermalExpansionCoefficient Coefficient, Temperature InitialTemperature, Temperature FinalTemperature)
{
    public override string ToString() => $"L0={InitialLength}, α={Coefficient}, {InitialTemperature}→{FinalTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConvectiveHeatTransferInput(HeatTransferCoefficient HeatTransferCoefficient, Area Area, Temperature SurfaceTemperature, Temperature FluidTemperature)
{
    public override string ToString() => $"h={HeatTransferCoefficient}, A={Area}, Ts={SurfaceTemperature}, T∞={FluidTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RadiativeHeatTransferInput(double Emissivity, Area Area, Temperature HotTemperature, Temperature ColdTemperature)
{
    public double Emissivity { get; } = Emissivity is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(Emissivity)) : Emissivity;

    public override string ToString() => $"ε={Emissivity}, A={Area}, {HotTemperature}/{ColdTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatExchangerInput(
    MassFlowRate HotMassFlowRate,
    SpecificHeatCapacity HotSpecificHeatCapacity,
    Temperature HotInletTemperature,
    MassFlowRate ColdMassFlowRate,
    SpecificHeatCapacity ColdSpecificHeatCapacity,
    Temperature ColdInletTemperature,
    double Effectiveness)
{
    public double Effectiveness { get; } = Effectiveness is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(Effectiveness)) : Effectiveness;

    public override string ToString() => $"ε={Effectiveness}, hot ṁ={HotMassFlowRate}, cold ṁ={ColdMassFlowRate}, Thi={HotInletTemperature}, Tci={ColdInletTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatExchangerResult(Energy HeatTransferred, Temperature HotOutletTemperature, Temperature ColdOutletTemperature)
{
    public override string ToString() => $"Q={HeatTransferred}, Th,out={HotOutletTemperature}, Tc,out={ColdOutletTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConvectionCorrelationInput(
    double ReynoldsNumber,
    double PrandtlNumber,
    Length CharacteristicLength,
    ThermalConductivity ThermalConductivity,
    bool IsHeating)
{
    public double ReynoldsNumber { get; } = ReynoldsNumber < 0d ? throw new ArgumentOutOfRangeException(nameof(ReynoldsNumber)) : ReynoldsNumber;

    public double PrandtlNumber { get; } = PrandtlNumber <= 0d ? throw new ArgumentOutOfRangeException(nameof(PrandtlNumber)) : PrandtlNumber;

    public override string ToString() => $"Re={ReynoldsNumber}, Pr={PrandtlNumber}, L={CharacteristicLength}, k={ThermalConductivity}, heating={IsHeating}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NaturalConvectionInput(
    Acceleration Gravity,
    ThermalExpansionCoefficient ThermalExpansionCoefficient,
    Temperature SurfaceTemperature,
    Temperature FluidTemperature,
    Length CharacteristicLength,
    KinematicViscosity KinematicViscosity,
    double PrandtlNumber)
{
    public double PrandtlNumber { get; } = PrandtlNumber <= 0d ? throw new ArgumentOutOfRangeException(nameof(PrandtlNumber)) : PrandtlNumber;

    public override string ToString() => $"g={Gravity}, L={CharacteristicLength}, Pr={PrandtlNumber}, Ts={SurfaceTemperature}, T∞={FluidTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RadiationExchangeInput(
    double EmissivityHot,
    double EmissivityCold,
    Area Area,
    Temperature HotTemperature,
    Temperature ColdTemperature,
    double ViewFactor)
{
    public double EmissivityHot { get; } = EmissivityHot is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(EmissivityHot)) : EmissivityHot;

    public double EmissivityCold { get; } = EmissivityCold is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(EmissivityCold)) : EmissivityCold;

    public double ViewFactor { get; } = ViewFactor is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(ViewFactor)) : ViewFactor;

    public override string ToString() => $"εh={EmissivityHot}, εc={EmissivityCold}, F={ViewFactor}, A={Area}, {HotTemperature}/{ColdTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FluidFlowState(Density Density, Velocity Velocity, Pressure Pressure, DynamicViscosity DynamicViscosity, Length HydraulicDiameter)
{
    public override string ToString() => $"ρ={Density}, v={Velocity}, P={Pressure}, μ={DynamicViscosity}, Dh={HydraulicDiameter}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ReynoldsNumberInput(FluidFlowState State)
{
    public override string ToString() => $"Re input: {State}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DragForceInput(Density FluidDensity, Velocity Velocity, double DragCoefficient, Area ReferenceArea)
{
    public double DragCoefficient { get; } = DragCoefficient < 0d ? throw new ArgumentOutOfRangeException(nameof(DragCoefficient)) : DragCoefficient;

    public override string ToString() => $"ρ={FluidDensity}, v={Velocity}, Cd={DragCoefficient}, A={ReferenceArea}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BuoyancyInput(Density FluidDensity, Volume DisplacedVolume, Acceleration Gravity)
{
    public override string ToString() => $"ρ={FluidDensity}, V={DisplacedVolume}, g={Gravity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PipeFlowInput(FluidFlowState State, Length PipeLength, double FrictionFactor)
{
    public double FrictionFactor { get; } = FrictionFactor < 0d ? throw new ArgumentOutOfRangeException(nameof(FrictionFactor)) : FrictionFactor;

    public override string ToString() => $"L={PipeLength}, f={FrictionFactor}, {State}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CompressibleFlowInput(Pressure StagnationPressure, Temperature StagnationTemperature, double SpecificHeatRatio, double GasConstant, double MachNumber)
{
    public double SpecificHeatRatio { get; } = SpecificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(SpecificHeatRatio)) : SpecificHeatRatio;

    public double GasConstant { get; } = GasConstant <= 0d ? throw new ArgumentOutOfRangeException(nameof(GasConstant)) : GasConstant;

    public double MachNumber { get; } = MachNumber < 0d ? throw new ArgumentOutOfRangeException(nameof(MachNumber)) : MachNumber;

    public override string ToString() => $"P0={StagnationPressure}, T0={StagnationTemperature}, γ={SpecificHeatRatio}, R={GasConstant}, M={MachNumber}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NozzleFlowInput(Pressure ChamberPressure, Temperature ChamberTemperature, Pressure ExitPressure, double SpecificHeatRatio, double GasConstant)
{
    public double SpecificHeatRatio { get; } = SpecificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(SpecificHeatRatio)) : SpecificHeatRatio;

    public double GasConstant { get; } = GasConstant <= 0d ? throw new ArgumentOutOfRangeException(nameof(GasConstant)) : GasConstant;

    public override string ToString() => $"Pc={ChamberPressure}, Tc={ChamberTemperature}, Pe={ExitPressure}, γ={SpecificHeatRatio}, R={GasConstant}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NozzleFlowResult(Velocity ExitVelocity, MassFlowRate? ChokedMassFlowRate = null)
{
    public override string ToString() => $"ve={ExitVelocity}, ṁ*={ScientificModelDisplay.NullProp(ChokedMassFlowRate, static m => m.ToString())}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NormalShockInput(double UpstreamMachNumber, double SpecificHeatRatio)
{
    public double UpstreamMachNumber { get; } = UpstreamMachNumber <= 1d ? throw new ArgumentOutOfRangeException(nameof(UpstreamMachNumber)) : UpstreamMachNumber;

    public double SpecificHeatRatio { get; } = SpecificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(SpecificHeatRatio)) : SpecificHeatRatio;

    public override string ToString() => $"M1={UpstreamMachNumber}, γ={SpecificHeatRatio}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ObliqueShockInput(double UpstreamMachNumber, Angle ShockAngle, double SpecificHeatRatio)
{
    public double UpstreamMachNumber { get; } = UpstreamMachNumber <= 1d ? throw new ArgumentOutOfRangeException(nameof(UpstreamMachNumber)) : UpstreamMachNumber;

    public double SpecificHeatRatio { get; } = SpecificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(SpecificHeatRatio)) : SpecificHeatRatio;

    public override string ToString() => $"M1={UpstreamMachNumber}, θ={ShockAngle}, γ={SpecificHeatRatio}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ObliqueShockResult(double NormalMachBefore, double NormalMachAfter, double PressureRatio, Angle FlowDeflectionAngle)
{
    public override string ToString() => $"Mn1={NormalMachBefore}, Mn2={NormalMachAfter}, p2/p1={PressureRatio}, δ={FlowDeflectionAngle}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RotationalInertiaInput(Mass Mass, Length CharacteristicLength)
{
    public override string ToString() => $"m={Mass}, L={CharacteristicLength}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RotationalEnergyInput(MomentOfInertia MomentOfInertia, AngularVelocity AngularVelocity)
{
    public override string ToString() => $"I={MomentOfInertia}, ω={AngularVelocity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularMomentumInput(MomentOfInertia MomentOfInertia, AngularVelocity AngularVelocity)
{
    public override string ToString() => $"I={MomentOfInertia}, ω={AngularVelocity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringOscillatorInput(Mass Mass, SpringConstant SpringConstant)
{
    public override string ToString() => $"m={Mass}, k={SpringConstant}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PendulumInput(Length Length, Acceleration Gravity)
{
    public override string ToString() => $"L={Length}, g={Gravity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct StressStrainInput(Force Force, Area CrossSectionArea, Length OriginalLength, Length LengthChange)
{
    public override string ToString() => $"F={Force}, A={CrossSectionArea}, L0={OriginalLength}, ΔL={LengthChange}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BeamBendingInput(
    Force Force,
    Length BeamLength,
    ModulusOfElasticity ModulusOfElasticity,
    AreaMomentOfInertia AreaMomentOfInertia,
    Length DistanceFromNeutralAxis)
{
    public override string ToString() => $"F={Force}, L={BeamLength}, E={ModulusOfElasticity}, I={AreaMomentOfInertia}, y={DistanceFromNeutralAxis}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FractureInput(Pressure AppliedStress, Length CrackLength, FractureToughness FractureToughness)
{
    public override string ToString() => $"σ={AppliedStress}, a={CrackLength}, KIc={FractureToughness}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RectangularSectionInput(Length Width, Length Height)
{
    public override string ToString() => $"w={Width}, h={Height}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CircularSectionInput(Length Radius)
{
    public override string ToString() => $"r={Radius}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FatigueInput(Pressure AlternatingStress, Pressure MeanStress, Pressure EnduranceLimit, Pressure UltimateStrength)
{
    public override string ToString() => $"σa={AlternatingStress}, σm={MeanStress}, Se={EnduranceLimit}, Su={UltimateStrength}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SNCurveInput(Pressure StressAmplitude, double FatigueStrengthCoefficient, double FatigueStrengthExponent)
{
    public double FatigueStrengthCoefficient { get; } =
        FatigueStrengthCoefficient <= 0d ? throw new ArgumentOutOfRangeException(nameof(FatigueStrengthCoefficient)) : FatigueStrengthCoefficient;

    public double FatigueStrengthExponent { get; } =
        FatigueStrengthExponent >= 0d ? throw new ArgumentOutOfRangeException(nameof(FatigueStrengthExponent)) : FatigueStrengthExponent;

    public override string ToString() => $"σa={StressAmplitude}, σf'={FatigueStrengthCoefficient}, b={FatigueStrengthExponent}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BeamSectionProfile(string Name, Area CrossSectionArea, AreaMomentOfInertia AreaMomentOfInertia, Length DistanceFromNeutralAxis)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, A={CrossSectionArea}, I={AreaMomentOfInertia}, y={DistanceFromNeutralAxis}";
}

public static class BeamSectionCatalog
{
    public static IReadOnlyList<BeamSectionProfile> Common { get; } = [
        new("Rectangular 100x200 mm", Area.FromSquareMeters(0.02d), AreaMomentOfInertia.FromMetersToFourth(6.666666666666667e-5d), Length.FromMeters(0.1d)),
        new("Square Tube 50x50x5 mm", Area.FromSquareMeters(9.0e-4d), AreaMomentOfInertia.FromMetersToFourth(3.075e-7d), Length.FromMeters(0.025d)),
        new("Circular 100 mm", Area.FromSquareMeters(Math.PI * 0.05d * 0.05d), AreaMomentOfInertia.FromMetersToFourth(Math.PI * Math.Pow(0.05d, 4d) / 4d), Length.FromMeters(0.05d))
    ];
}

public static class EngineeringMaterials
{
    public static IReadOnlyList<MaterialProperty> Common { get; } = [
        new(
            "Air", Density.FromKilogramsPerCubicMeter(1.225d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(1005d), ThermalConductivity.FromWattsPerMeterKelvin(0.024d),
            DynamicViscosity.FromPascalSeconds(1.81e-5d)),
        new(
            "Water", Density.FromKilogramsPerCubicMeter(997d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(4184d), ThermalConductivity.FromWattsPerMeterKelvin(0.598d),
            DynamicViscosity.FromPascalSeconds(8.9e-4d), ThermalExpansionCoefficient.FromPerKelvin(2.07e-4d)),
        new(
            "Steel", Density.FromKilogramsPerCubicMeter(7850d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(490d), ThermalConductivity.FromWattsPerMeterKelvin(43d), null,
            ThermalExpansionCoefficient.FromPerKelvin(12e-6d), ModulusOfElasticity.FromPascals(200e9d), Pressure.FromPascals(250e6d),
            FractureToughness.FromPascalRootMeters(50e6d)),
        new(
            "Aluminum", Density.FromKilogramsPerCubicMeter(2700d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(897d), ThermalConductivity.FromWattsPerMeterKelvin(205d), null,
            ThermalExpansionCoefficient.FromPerKelvin(23.1e-6d), ModulusOfElasticity.FromPascals(69e9d), Pressure.FromPascals(95e6d),
            FractureToughness.FromPascalRootMeters(29e6d)),
        new(
            "Copper", Density.FromKilogramsPerCubicMeter(8960d), SpecificHeatCapacity.FromJoulesPerKilogramKelvin(385d), ThermalConductivity.FromWattsPerMeterKelvin(401d), null,
            ThermalExpansionCoefficient.FromPerKelvin(16.5e-6d), ModulusOfElasticity.FromPascals(110e9d), Pressure.FromPascals(70e6d),
            FractureToughness.FromPascalRootMeters(36e6d))
    ];
}