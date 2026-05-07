using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Engineering;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MaterialProperty
{

    public MaterialProperty(string name, Density density, SpecificHeatCapacity specificHeatCapacity, ThermalConductivity thermalConductivity, DynamicViscosity? dynamicViscosity = null, ThermalExpansionCoefficient? thermalExpansionCoefficient = null, ModulusOfElasticity? modulusOfElasticity = null, Pressure? yieldStrength = null, FractureToughness? fractureToughness = null)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;

        this.Name = name;
        this.Density = density;
        this.SpecificHeatCapacity = specificHeatCapacity;
        this.ThermalConductivity = thermalConductivity;
        this.DynamicViscosity = dynamicViscosity;
        this.ThermalExpansionCoefficient = thermalExpansionCoefficient;
        this.ModulusOfElasticity = modulusOfElasticity;
        this.YieldStrength = yieldStrength;
        this.FractureToughness = fractureToughness;
}


    public string Name { get;  init; }

    public Density Density { get; init; }
    public SpecificHeatCapacity SpecificHeatCapacity { get; init; }
    public ThermalConductivity ThermalConductivity { get; init; }
    public DynamicViscosity? DynamicViscosity { get; init; }
    public ThermalExpansionCoefficient? ThermalExpansionCoefficient { get; init; }
    public ModulusOfElasticity? ModulusOfElasticity { get; init; }
    public Pressure? YieldStrength { get; init; }
    public FractureToughness? FractureToughness { get; init; }
    public override string ToString() => $"{Name}, ρ={Density}, cp={SpecificHeatCapacity}, k={ThermalConductivity}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermodynamicState
{

    public ThermodynamicState(Temperature temperature, Pressure pressure, Volume volume, Mass? mass = null, double? moles = null)

    {

        moles = moles is not null && moles < 0d ? throw new ArgumentOutOfRangeException(nameof(moles)) : moles;

    
        this.Temperature = temperature;
        this.Pressure = pressure;
        this.Volume = volume;
        this.Mass = mass;
        Moles = moles;
}


    public double? Moles { get;  }

    public Temperature Temperature { get; }
    public Pressure Pressure { get; }
    public Volume Volume { get; }
    public Mass? Mass { get; }
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
public readonly record struct RadiativeHeatTransferInput
{

    public RadiativeHeatTransferInput(double emissivity, Area area, Temperature hotTemperature, Temperature coldTemperature)

    {

        emissivity = emissivity is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(emissivity)) : emissivity;

    
        this.Area = area;
        this.HotTemperature = hotTemperature;
        this.ColdTemperature = coldTemperature;
        Emissivity = emissivity;
}


    public double Emissivity { get;  }

    public Area Area { get; }
    public Temperature HotTemperature { get; }
    public Temperature ColdTemperature { get; }
    public override string ToString() => $"ε={Emissivity}, A={Area}, {HotTemperature}/{ColdTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatExchangerInput
{

    public HeatExchangerInput(MassFlowRate hotMassFlowRate, SpecificHeatCapacity hotSpecificHeatCapacity, Temperature hotInletTemperature, MassFlowRate coldMassFlowRate, SpecificHeatCapacity coldSpecificHeatCapacity, Temperature coldInletTemperature, double effectiveness)

    {

        effectiveness = effectiveness is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(effectiveness)) : effectiveness;

    
        this.HotMassFlowRate = hotMassFlowRate;
        this.HotSpecificHeatCapacity = hotSpecificHeatCapacity;
        this.HotInletTemperature = hotInletTemperature;
        this.ColdMassFlowRate = coldMassFlowRate;
        this.ColdSpecificHeatCapacity = coldSpecificHeatCapacity;
        this.ColdInletTemperature = coldInletTemperature;
        Effectiveness = effectiveness;
}


    public double Effectiveness { get;  }

    public MassFlowRate HotMassFlowRate { get; }
    public SpecificHeatCapacity HotSpecificHeatCapacity { get; }
    public Temperature HotInletTemperature { get; }
    public MassFlowRate ColdMassFlowRate { get; }
    public SpecificHeatCapacity ColdSpecificHeatCapacity { get; }
    public Temperature ColdInletTemperature { get; }
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

    public ConvectionCorrelationInput(double reynoldsNumber, double prandtlNumber, Length characteristicLength, ThermalConductivity thermalConductivity, bool isHeating)

    {

        reynoldsNumber = reynoldsNumber < 0d ? throw new ArgumentOutOfRangeException(nameof(reynoldsNumber)) : reynoldsNumber;

        prandtlNumber = prandtlNumber <= 0d ? throw new ArgumentOutOfRangeException(nameof(prandtlNumber)) : prandtlNumber;

    
        this.CharacteristicLength = characteristicLength;
        this.ThermalConductivity = thermalConductivity;
        this.IsHeating = isHeating;
        ReynoldsNumber = reynoldsNumber;
        PrandtlNumber = prandtlNumber;
}


    public double ReynoldsNumber { get;  }
    public double PrandtlNumber { get;  }

    public Length CharacteristicLength { get; }
    public ThermalConductivity ThermalConductivity { get; }
    public bool IsHeating { get; }
    public override string ToString() => $"Re={ReynoldsNumber}, Pr={PrandtlNumber}, L={CharacteristicLength}, k={ThermalConductivity}, heating={IsHeating}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NaturalConvectionInput
{

    public NaturalConvectionInput(Acceleration gravity, ThermalExpansionCoefficient thermalExpansionCoefficient, Temperature surfaceTemperature, Temperature fluidTemperature, Length characteristicLength, KinematicViscosity kinematicViscosity, double prandtlNumber)

    {

        prandtlNumber = prandtlNumber <= 0d ? throw new ArgumentOutOfRangeException(nameof(prandtlNumber)) : prandtlNumber;

    
        this.Gravity = gravity;
        this.ThermalExpansionCoefficient = thermalExpansionCoefficient;
        this.SurfaceTemperature = surfaceTemperature;
        this.FluidTemperature = fluidTemperature;
        this.CharacteristicLength = characteristicLength;
        this.KinematicViscosity = kinematicViscosity;
        PrandtlNumber = prandtlNumber;
}


    public double PrandtlNumber { get;  }

    public Acceleration Gravity { get; }
    public ThermalExpansionCoefficient ThermalExpansionCoefficient { get; }
    public Temperature SurfaceTemperature { get; }
    public Temperature FluidTemperature { get; }
    public Length CharacteristicLength { get; }
    public KinematicViscosity KinematicViscosity { get; }
    public override string ToString() => $"g={Gravity}, L={CharacteristicLength}, Pr={PrandtlNumber}, Ts={SurfaceTemperature}, T∞={FluidTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RadiationExchangeInput
{

    public RadiationExchangeInput(double emissivityHot, double emissivityCold, Area area, Temperature hotTemperature, Temperature coldTemperature, double viewFactor)

    {

        emissivityHot = emissivityHot is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(emissivityHot)) : emissivityHot;

        emissivityCold = emissivityCold is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(emissivityCold)) : emissivityCold;

        viewFactor = viewFactor is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(viewFactor)) : viewFactor;

    
        this.Area = area;
        this.HotTemperature = hotTemperature;
        this.ColdTemperature = coldTemperature;
        EmissivityHot = emissivityHot;
        EmissivityCold = emissivityCold;
        ViewFactor = viewFactor;
}


    public double EmissivityHot { get;  }
    public double EmissivityCold { get;  }
    public double ViewFactor { get;  }

    public Area Area { get; }
    public Temperature HotTemperature { get; }
    public Temperature ColdTemperature { get; }
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

    public DragForceInput(Density fluidDensity, Velocity velocity, double dragCoefficient, Area referenceArea)

    {

        dragCoefficient = dragCoefficient < 0d ? throw new ArgumentOutOfRangeException(nameof(dragCoefficient)) : dragCoefficient;

    
        this.FluidDensity = fluidDensity;
        this.Velocity = velocity;
        this.ReferenceArea = referenceArea;
        DragCoefficient = dragCoefficient;
}


    public double DragCoefficient { get;  }

    public Density FluidDensity { get; }
    public Velocity Velocity { get; }
    public Area ReferenceArea { get; }
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

    public PipeFlowInput(FluidFlowState state, Length pipeLength, double frictionFactor)

    {

        frictionFactor = frictionFactor < 0d ? throw new ArgumentOutOfRangeException(nameof(frictionFactor)) : frictionFactor;

    
        this.State = state;
        this.PipeLength = pipeLength;
        FrictionFactor = frictionFactor;
}


    public double FrictionFactor { get;  }

    public FluidFlowState State { get; }
    public Length PipeLength { get; }
    public override string ToString() => $"L={PipeLength}, f={FrictionFactor}, {State}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CompressibleFlowInput
{

    public CompressibleFlowInput(Pressure stagnationPressure, Temperature stagnationTemperature, double specificHeatRatio, double gasConstant, double machNumber)

    {

        specificHeatRatio = specificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(specificHeatRatio)) : specificHeatRatio;

        gasConstant = gasConstant <= 0d ? throw new ArgumentOutOfRangeException(nameof(gasConstant)) : gasConstant;

        machNumber = machNumber < 0d ? throw new ArgumentOutOfRangeException(nameof(machNumber)) : machNumber;

    
        this.StagnationPressure = stagnationPressure;
        this.StagnationTemperature = stagnationTemperature;
        SpecificHeatRatio = specificHeatRatio;
        GasConstant = gasConstant;
        MachNumber = machNumber;
}


    public double SpecificHeatRatio { get;  }
    public double GasConstant { get;  }
    public double MachNumber { get;  }

    public Pressure StagnationPressure { get; }
    public Temperature StagnationTemperature { get; }
    public override string ToString() => $"P0={StagnationPressure}, T0={StagnationTemperature}, γ={SpecificHeatRatio}, R={GasConstant}, M={MachNumber}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NozzleFlowInput
{

    public NozzleFlowInput(Pressure chamberPressure, Temperature chamberTemperature, Pressure exitPressure, double specificHeatRatio, double gasConstant)

    {

        specificHeatRatio = specificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(specificHeatRatio)) : specificHeatRatio;

        gasConstant = gasConstant <= 0d ? throw new ArgumentOutOfRangeException(nameof(gasConstant)) : gasConstant;

    
        this.ChamberPressure = chamberPressure;
        this.ChamberTemperature = chamberTemperature;
        this.ExitPressure = exitPressure;
        SpecificHeatRatio = specificHeatRatio;
        GasConstant = gasConstant;
}


    public double SpecificHeatRatio { get;  }
    public double GasConstant { get;  }

    public Pressure ChamberPressure { get; }
    public Temperature ChamberTemperature { get; }
    public Pressure ExitPressure { get; }
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

    public NormalShockInput(double upstreamMachNumber, double specificHeatRatio)

    {

        upstreamMachNumber = upstreamMachNumber <= 1d ? throw new ArgumentOutOfRangeException(nameof(upstreamMachNumber)) : upstreamMachNumber;

        specificHeatRatio = specificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(specificHeatRatio)) : specificHeatRatio;
        UpstreamMachNumber = upstreamMachNumber;
        SpecificHeatRatio = specificHeatRatio;
}


    public double UpstreamMachNumber { get;  }
    public double SpecificHeatRatio { get;  }
    public override string ToString() => $"M1={UpstreamMachNumber}, γ={SpecificHeatRatio}";
}

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ObliqueShockInput
{

    public ObliqueShockInput(double upstreamMachNumber, Angle shockAngle, double specificHeatRatio)

    {

        upstreamMachNumber = upstreamMachNumber <= 1d ? throw new ArgumentOutOfRangeException(nameof(upstreamMachNumber)) : upstreamMachNumber;

        specificHeatRatio = specificHeatRatio <= 1d ? throw new ArgumentOutOfRangeException(nameof(specificHeatRatio)) : specificHeatRatio;

    
        this.ShockAngle = shockAngle;
        UpstreamMachNumber = upstreamMachNumber;
        SpecificHeatRatio = specificHeatRatio;
}


    public double UpstreamMachNumber { get;  }
    public double SpecificHeatRatio { get;  }

    public Angle ShockAngle { get; }
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

    public SNCurveInput(Pressure stressAmplitude, double fatigueStrengthCoefficient, double fatigueStrengthExponent)

    {

        fatigueStrengthCoefficient = fatigueStrengthCoefficient <= 0d ? throw new ArgumentOutOfRangeException(nameof(fatigueStrengthCoefficient)) : fatigueStrengthCoefficient;

        fatigueStrengthExponent = fatigueStrengthExponent >= 0d ? throw new ArgumentOutOfRangeException(nameof(fatigueStrengthExponent)) : fatigueStrengthExponent;

    
        this.StressAmplitude = stressAmplitude;
        FatigueStrengthCoefficient = fatigueStrengthCoefficient;
        FatigueStrengthExponent = fatigueStrengthExponent;
}


    public double FatigueStrengthCoefficient { get;  }
    public double FatigueStrengthExponent { get;  }

    public Pressure StressAmplitude { get; }
    public override string ToString() => $"σa={StressAmplitude}, σf'={FatigueStrengthCoefficient}, b={FatigueStrengthExponent}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BeamSectionProfile
{

    public BeamSectionProfile(string name, Area crossSectionArea, AreaMomentOfInertia areaMomentOfInertia, Length distanceFromNeutralAxis)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;

        this.Name = name;
        this.CrossSectionArea = crossSectionArea;
        this.AreaMomentOfInertia = areaMomentOfInertia;
        this.DistanceFromNeutralAxis = distanceFromNeutralAxis;
}


    public string Name { get;  init; }

    public Area CrossSectionArea { get; init; }
    public AreaMomentOfInertia AreaMomentOfInertia { get; init; }
    public Length DistanceFromNeutralAxis { get; init; }
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