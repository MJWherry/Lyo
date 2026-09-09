using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Engineering;

/// <summary>Engineering contracts for thermodynamics, fluid flow, heat transfer, and solid mechanics, built on <c>Lyo.Mathematics.Quantities</c>.</summary>
/// <remarks>Heavy numerical work lives in <c>Lyo.Scientific.Functions</c>; this file holds typed inputs and small reference catalogs.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MaterialProperty
{
    /// <summary>Display name for the material (for example <c>Steel</c>, <c>Water</c>).</summary>
    public string Name { get; init; }

    /// <summary>Mass density used by buoyancy and inertia calculations.</summary>
    public Density Density { get; init; }

    /// <summary>Specific heat capacity at constant pressure.</summary>
    public SpecificHeatCapacity SpecificHeatCapacity { get; init; }

    /// <summary>Thermal conductivity used by Fourier conduction models.</summary>
    public ThermalConductivity ThermalConductivity { get; init; }

    /// <summary>Optional dynamic viscosity for viscous-flow correlations; omit when unused.</summary>
    public DynamicViscosity? DynamicViscosity { get; init; }

    /// <summary>Optional linear thermal-expansion coefficient; omit when unused.</summary>
    public ThermalExpansionCoefficient? ThermalExpansionCoefficient { get; init; }

    /// <summary>Optional Young’s modulus for elastic-deformation estimates; omit when unused.</summary>
    public ModulusOfElasticity? ModulusOfElasticity { get; init; }

    /// <summary>Optional yield strength for strength-of-materials comparisons; omit when unused.</summary>
    public Pressure? YieldStrength { get; init; }

    /// <summary>Optional fracture toughness for crack-sensitive models; omit when unused.</summary>
    public FractureToughness? FractureToughness { get; init; }

    public MaterialProperty(
        string name,
        Density density,
        SpecificHeatCapacity specificHeatCapacity,
        ThermalConductivity thermalConductivity,
        DynamicViscosity? dynamicViscosity = null,
        ThermalExpansionCoefficient? thermalExpansionCoefficient = null,
        ModulusOfElasticity? modulusOfElasticity = null,
        Pressure? yieldStrength = null,
        FractureToughness? fractureToughness = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        Density = density;
        SpecificHeatCapacity = specificHeatCapacity;
        ThermalConductivity = thermalConductivity;
        DynamicViscosity = dynamicViscosity;
        ThermalExpansionCoefficient = thermalExpansionCoefficient;
        ModulusOfElasticity = modulusOfElasticity;
        YieldStrength = yieldStrength;
        FractureToughness = fractureToughness;
    }

    public override string ToString() => $"{Name}, ρ={Density}, cp={SpecificHeatCapacity}, k={ThermalConductivity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermodynamicState
{
    /// <summary>Optional mole count for this thermodynamic state.</summary>
    public double? Moles { get; }

    /// <summary>Absolute temperature of the state.</summary>
    public Temperature Temperature { get; }

    /// <summary>Absolute pressure of the state.</summary>
    public Pressure Pressure { get; }

    /// <summary>Occupying volume of the state.</summary>
    public Volume Volume { get; }

    /// <summary>Optional mass associated with the state.</summary>
    public Mass? Mass { get; }

    public ThermodynamicState(Temperature temperature, Pressure pressure, Volume volume, Mass? mass = null, double? moles = null)
    {
        if (moles.HasValue)
            ArgumentHelpers.ThrowIfLessThan(moles.Value, 0d, nameof(moles));

        Temperature = temperature;
        Pressure = pressure;
        Volume = volume;
        Mass = mass;
        Moles = moles;
    }

    public override string ToString()
        => $"T={Temperature}, P={Pressure}, V={Volume}, m={ScientificModelDisplay.NullProp(Mass, static m => m.ToString())}, n={ScientificModelDisplay.NullProp(Moles, static n => n.ToString(CultureInfo.CurrentCulture))}";
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
public readonly record struct RadiativeHeatTransferInput
{
    /// <summary>Surface emissivity, typically in <c>[0, 1]</c>.</summary>
    public double Emissivity { get; }

    /// <summary>Participating surface area.</summary>
    public Area Area { get; }

    /// <summary>Hot-side temperature.</summary>
    public Temperature HotTemperature { get; }

    /// <summary>Cold-side temperature.</summary>
    public Temperature ColdTemperature { get; }

    public RadiativeHeatTransferInput(double emissivity, Area area, Temperature hotTemperature, Temperature coldTemperature)
    {
        ArgumentHelpers.ThrowIfNotInRange(emissivity, 0d, 1d);
        Area = area;
        HotTemperature = hotTemperature;
        ColdTemperature = coldTemperature;
        Emissivity = emissivity;
    }

    public override string ToString() => $"ε={Emissivity}, A={Area}, {HotTemperature}/{ColdTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatExchangerInput
{
    /// <summary>Heat-exchanger effectiveness, typically in <c>[0, 1]</c>.</summary>
    public double Effectiveness { get; }

    /// <summary>Hot-stream mass flow rate.</summary>
    public MassFlowRate HotMassFlowRate { get; }

    /// <summary>Hot-stream specific heat capacity.</summary>
    public SpecificHeatCapacity HotSpecificHeatCapacity { get; }

    /// <summary>Hot-stream inlet temperature.</summary>
    public Temperature HotInletTemperature { get; }

    /// <summary>Cold-stream mass flow rate.</summary>
    public MassFlowRate ColdMassFlowRate { get; }

    /// <summary>Cold-stream specific heat capacity.</summary>
    public SpecificHeatCapacity ColdSpecificHeatCapacity { get; }

    /// <summary>Cold-stream inlet temperature.</summary>
    public Temperature ColdInletTemperature { get; }

    public HeatExchangerInput(
        MassFlowRate hotMassFlowRate,
        SpecificHeatCapacity hotSpecificHeatCapacity,
        Temperature hotInletTemperature,
        MassFlowRate coldMassFlowRate,
        SpecificHeatCapacity coldSpecificHeatCapacity,
        Temperature coldInletTemperature,
        double effectiveness)
    {
        ArgumentHelpers.ThrowIfNotInRange(effectiveness, 0d, 1d);
        HotMassFlowRate = hotMassFlowRate;
        HotSpecificHeatCapacity = hotSpecificHeatCapacity;
        HotInletTemperature = hotInletTemperature;
        ColdMassFlowRate = coldMassFlowRate;
        ColdSpecificHeatCapacity = coldSpecificHeatCapacity;
        ColdInletTemperature = coldInletTemperature;
        Effectiveness = effectiveness;
    }

    public override string ToString() => $"ε={Effectiveness}, hot ṁ={HotMassFlowRate}, cold ṁ={ColdMassFlowRate}, Thi={HotInletTemperature}, Tci={ColdInletTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatExchangerResult(Energy HeatTransferred, Temperature HotOutletTemperature, Temperature ColdOutletTemperature)
{
    public override string ToString() => $"Q={HeatTransferred}, Th,out={HotOutletTemperature}, Tc,out={ColdOutletTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConvectionCorrelationInput
{
    /// <summary>Reynolds number of the flow.</summary>
    public double ReynoldsNumber { get; }

    /// <summary>Prandtl number of the fluid.</summary>
    public double PrandtlNumber { get; }

    /// <summary>Characteristic length used by the correlation.</summary>
    public Length CharacteristicLength { get; }

    /// <summary>Fluid thermal conductivity.</summary>
    public ThermalConductivity ThermalConductivity { get; }

    /// <summary>True when the wall is heating the fluid.</summary>
    public bool IsHeating { get; }

    public ConvectionCorrelationInput(double reynoldsNumber, double prandtlNumber, Length characteristicLength, ThermalConductivity thermalConductivity, bool isHeating)
    {
        ArgumentHelpers.ThrowIfLessThan(reynoldsNumber, 0d);
        ArgumentHelpers.ThrowIfLessThanOrEqual(prandtlNumber, 0d);
        CharacteristicLength = characteristicLength;
        ThermalConductivity = thermalConductivity;
        IsHeating = isHeating;
        ReynoldsNumber = reynoldsNumber;
        PrandtlNumber = prandtlNumber;
    }

    public override string ToString() => $"Re={ReynoldsNumber}, Pr={PrandtlNumber}, L={CharacteristicLength}, k={ThermalConductivity}, heating={IsHeating}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NaturalConvectionInput
{
    /// <summary>Prandtl number of the fluid.</summary>
    public double PrandtlNumber { get; }

    /// <summary>Gravitational acceleration used by the buoyancy term.</summary>
    public Acceleration Gravity { get; }

    /// <summary>Fluid thermal-expansion coefficient.</summary>
    public ThermalExpansionCoefficient ThermalExpansionCoefficient { get; }

    /// <summary>Solid-surface temperature.</summary>
    public Temperature SurfaceTemperature { get; }

    /// <summary>Far-field fluid temperature.</summary>
    public Temperature FluidTemperature { get; }

    /// <summary>Characteristic length used by the correlation.</summary>
    public Length CharacteristicLength { get; }

    /// <summary>Fluid kinematic viscosity.</summary>
    public KinematicViscosity KinematicViscosity { get; }

    public NaturalConvectionInput(
        Acceleration gravity,
        ThermalExpansionCoefficient thermalExpansionCoefficient,
        Temperature surfaceTemperature,
        Temperature fluidTemperature,
        Length characteristicLength,
        KinematicViscosity kinematicViscosity,
        double prandtlNumber)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(prandtlNumber, 0d);
        Gravity = gravity;
        ThermalExpansionCoefficient = thermalExpansionCoefficient;
        SurfaceTemperature = surfaceTemperature;
        FluidTemperature = fluidTemperature;
        CharacteristicLength = characteristicLength;
        KinematicViscosity = kinematicViscosity;
        PrandtlNumber = prandtlNumber;
    }

    public override string ToString() => $"g={Gravity}, L={CharacteristicLength}, Pr={PrandtlNumber}, Ts={SurfaceTemperature}, T∞={FluidTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RadiationExchangeInput
{
    /// <summary>Hot-surface emissivity, typically in <c>[0, 1]</c>.</summary>
    public double EmissivityHot { get; }

    /// <summary>Cold-surface emissivity, typically in <c>[0, 1]</c>.</summary>
    public double EmissivityCold { get; }

    /// <summary>Radiation view factor between the two surfaces.</summary>
    public double ViewFactor { get; }

    /// <summary>Participating surface area.</summary>
    public Area Area { get; }

    /// <summary>Hot-side temperature.</summary>
    public Temperature HotTemperature { get; }

    /// <summary>Cold-side temperature.</summary>
    public Temperature ColdTemperature { get; }

    public RadiationExchangeInput(double emissivityHot, double emissivityCold, Area area, Temperature hotTemperature, Temperature coldTemperature, double viewFactor)
    {
        ArgumentHelpers.ThrowIfNotInRange(emissivityHot, 0d, 1d);
        ArgumentHelpers.ThrowIfNotInRange(emissivityCold, 0d, 1d);
        ArgumentHelpers.ThrowIfNotInRange(viewFactor, 0d, 1d);
        Area = area;
        HotTemperature = hotTemperature;
        ColdTemperature = coldTemperature;
        EmissivityHot = emissivityHot;
        EmissivityCold = emissivityCold;
        ViewFactor = viewFactor;
    }

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
public readonly record struct DragForceInput
{
    /// <summary>Dimensionless drag coefficient.</summary>
    public double DragCoefficient { get; }

    /// <summary>Freestream fluid density.</summary>
    public Density FluidDensity { get; }

    /// <summary>Freestream velocity.</summary>
    public Velocity Velocity { get; }

    /// <summary>Reference area used by the drag equation.</summary>
    public Area ReferenceArea { get; }

    public DragForceInput(Density fluidDensity, Velocity velocity, double dragCoefficient, Area referenceArea)
    {
        ArgumentHelpers.ThrowIfLessThan(dragCoefficient, 0d);
        FluidDensity = fluidDensity;
        Velocity = velocity;
        ReferenceArea = referenceArea;
        DragCoefficient = dragCoefficient;
    }

    public override string ToString() => $"ρ={FluidDensity}, v={Velocity}, Cd={DragCoefficient}, A={ReferenceArea}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BuoyancyInput(Density FluidDensity, Volume DisplacedVolume, Acceleration Gravity)
{
    public override string ToString() => $"ρ={FluidDensity}, V={DisplacedVolume}, g={Gravity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PipeFlowInput
{
    /// <summary>Darcy friction factor.</summary>
    public double FrictionFactor { get; }

    /// <summary>Flow state used to evaluate the pipe correlation.</summary>
    public FluidFlowState State { get; }

    /// <summary>Pipe length along the flow path.</summary>
    public Length PipeLength { get; }

    public PipeFlowInput(FluidFlowState state, Length pipeLength, double frictionFactor)
    {
        ArgumentHelpers.ThrowIfLessThan(frictionFactor, 0d);
        State = state;
        PipeLength = pipeLength;
        FrictionFactor = frictionFactor;
    }

    public override string ToString() => $"L={PipeLength}, f={FrictionFactor}, {State}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CompressibleFlowInput
{
    /// <summary>Ratio of specific heats γ.</summary>
    public double SpecificHeatRatio { get; }

    /// <summary>Specific gas constant.</summary>
    public double GasConstant { get; }

    /// <summary>Local Mach number.</summary>
    public double MachNumber { get; }

    /// <summary>Stagnation (total) pressure.</summary>
    public Pressure StagnationPressure { get; }

    /// <summary>Stagnation (total) temperature.</summary>
    public Temperature StagnationTemperature { get; }

    public CompressibleFlowInput(Pressure stagnationPressure, Temperature stagnationTemperature, double specificHeatRatio, double gasConstant, double machNumber)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(specificHeatRatio, 1d);
        ArgumentHelpers.ThrowIfLessThanOrEqual(gasConstant, 0d);
        ArgumentHelpers.ThrowIfLessThan(machNumber, 0d);
        StagnationPressure = stagnationPressure;
        StagnationTemperature = stagnationTemperature;
        SpecificHeatRatio = specificHeatRatio;
        GasConstant = gasConstant;
        MachNumber = machNumber;
    }

    public override string ToString() => $"P0={StagnationPressure}, T0={StagnationTemperature}, γ={SpecificHeatRatio}, R={GasConstant}, M={MachNumber}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NozzleFlowInput
{
    /// <summary>Ratio of specific heats γ.</summary>
    public double SpecificHeatRatio { get; }

    /// <summary>Specific gas constant.</summary>
    public double GasConstant { get; }

    /// <summary>Combustion-chamber pressure.</summary>
    public Pressure ChamberPressure { get; }

    /// <summary>Combustion-chamber temperature.</summary>
    public Temperature ChamberTemperature { get; }

    /// <summary>Nozzle-exit static pressure.</summary>
    public Pressure ExitPressure { get; }

    public NozzleFlowInput(Pressure chamberPressure, Temperature chamberTemperature, Pressure exitPressure, double specificHeatRatio, double gasConstant)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(specificHeatRatio, 1d);
        ArgumentHelpers.ThrowIfLessThanOrEqual(gasConstant, 0d);
        ChamberPressure = chamberPressure;
        ChamberTemperature = chamberTemperature;
        ExitPressure = exitPressure;
        SpecificHeatRatio = specificHeatRatio;
        GasConstant = gasConstant;
    }

    public override string ToString() => $"Pc={ChamberPressure}, Tc={ChamberTemperature}, Pe={ExitPressure}, γ={SpecificHeatRatio}, R={GasConstant}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NozzleFlowResult(Velocity ExitVelocity, MassFlowRate? ChokedMassFlowRate = null)
{
    public override string ToString() => $"ve={ExitVelocity}, ṁ*={ScientificModelDisplay.NullProp(ChokedMassFlowRate, static m => m.ToString())}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NormalShockInput
{
    /// <summary>Mach number ahead of the shock.</summary>
    public double UpstreamMachNumber { get; }

    /// <summary>Ratio of specific heats γ.</summary>
    public double SpecificHeatRatio { get; }

    public NormalShockInput(double upstreamMachNumber, double specificHeatRatio)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(upstreamMachNumber, 1d);
        ArgumentHelpers.ThrowIfLessThanOrEqual(specificHeatRatio, 1d);
        UpstreamMachNumber = upstreamMachNumber;
        SpecificHeatRatio = specificHeatRatio;
    }

    public override string ToString() => $"M1={UpstreamMachNumber}, γ={SpecificHeatRatio}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ObliqueShockInput
{
    /// <summary>Mach number ahead of the shock.</summary>
    public double UpstreamMachNumber { get; }

    /// <summary>Ratio of specific heats γ.</summary>
    public double SpecificHeatRatio { get; }

    /// <summary>Oblique-shock wave angle.</summary>
    public Angle ShockAngle { get; }

    public ObliqueShockInput(double upstreamMachNumber, Angle shockAngle, double specificHeatRatio)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(upstreamMachNumber, 1d);
        ArgumentHelpers.ThrowIfLessThanOrEqual(specificHeatRatio, 1d);
        ShockAngle = shockAngle;
        UpstreamMachNumber = upstreamMachNumber;
        SpecificHeatRatio = specificHeatRatio;
    }

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
public readonly record struct SNCurveInput
{
    /// <summary>Fatigue strength coefficient in the S-N relation.</summary>
    public double FatigueStrengthCoefficient { get; }

    /// <summary>Fatigue strength exponent in the S-N relation.</summary>
    public double FatigueStrengthExponent { get; }

    /// <summary>Alternating stress amplitude.</summary>
    public Pressure StressAmplitude { get; }

    public SNCurveInput(Pressure stressAmplitude, double fatigueStrengthCoefficient, double fatigueStrengthExponent)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(fatigueStrengthCoefficient, 0d);
        ArgumentHelpers.ThrowIfGreaterThanOrEqual(fatigueStrengthExponent, 0d);
        StressAmplitude = stressAmplitude;
        FatigueStrengthCoefficient = fatigueStrengthCoefficient;
        FatigueStrengthExponent = fatigueStrengthExponent;
    }

    public override string ToString() => $"σa={StressAmplitude}, σf'={FatigueStrengthCoefficient}, b={FatigueStrengthExponent}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BeamSectionProfile
{
    /// <summary>Display name for the structural member.</summary>
    public string Name { get; init; }

    /// <summary>Cross-sectional area of the member.</summary>
    public Area CrossSectionArea { get; init; }

    /// <summary>Second moment of area about the bending axis.</summary>
    public AreaMomentOfInertia AreaMomentOfInertia { get; init; }

    /// <summary>Distance from the neutral axis to the extreme fiber.</summary>
    public Length DistanceFromNeutralAxis { get; init; }

    public BeamSectionProfile(string name, Area crossSectionArea, AreaMomentOfInertia areaMomentOfInertia, Length distanceFromNeutralAxis)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        CrossSectionArea = crossSectionArea;
        AreaMomentOfInertia = areaMomentOfInertia;
        DistanceFromNeutralAxis = distanceFromNeutralAxis;
    }

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