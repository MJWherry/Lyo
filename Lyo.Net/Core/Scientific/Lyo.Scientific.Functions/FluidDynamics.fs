namespace Lyo.Scientific.Functions

open Lyo.Mathematics.Quantities
open Lyo.Scientific.Engineering

[<AbstractClass; Sealed>]
type FluidDynamicsFunctions private () =
    static member ReynoldsNumber(input: ReynoldsNumberInput) =
        let state = input.State
        state.Density.KilogramsPerCubicMeter
        * state.Velocity.MetersPerSecond
        * state.HydraulicDiameter.Meters
        / state.DynamicViscosity.PascalSeconds

    static member DynamicPressure(fluidDensity: Density, velocity: Velocity) =
        Pressure.FromPascals(
            0.5
            * fluidDensity.KilogramsPerCubicMeter
            * velocity.MetersPerSecond
            * velocity.MetersPerSecond
        )

    static member VolumetricFlowRate(area: Area, velocity: Velocity) =
        VolumetricFlowRate.FromCubicMetersPerSecond(area.SquareMeters * velocity.MetersPerSecond)

    static member VelocityFromFlowRate(flowRate: VolumetricFlowRate, area: Area) =
        let section = ScientificGuard.positiveFinite "area.SquareMeters" area.SquareMeters
        Velocity.FromMetersPerSecond(flowRate.CubicMetersPerSecond / section)

    static member DragForce(input: DragForceInput) =
        let dynamicPressure =
            FluidDynamicsFunctions.DynamicPressure(input.FluidDensity, input.Velocity)

        Force.FromNewtons(
            dynamicPressure.Pascals
            * input.DragCoefficient
            * input.ReferenceArea.SquareMeters
        )

    static member BuoyantForce(input: BuoyancyInput) =
        Force.FromNewtons(
            input.FluidDensity.KilogramsPerCubicMeter
            * input.DisplacedVolume.CubicMeters
            * input.Gravity.MetersPerSecondSquared
        )

    static member DarcyWeisbachPressureDrop(input: PipeFlowInput) =
        let length =
            ScientificGuard.positiveFinite "input.PipeLength.Meters" input.PipeLength.Meters

        let diameter =
            ScientificGuard.positiveFinite "input.State.HydraulicDiameter.Meters" input.State.HydraulicDiameter.Meters

        let density = input.State.Density.KilogramsPerCubicMeter
        let velocity = input.State.Velocity.MetersPerSecond

        Pressure.FromPascals(
            input.FrictionFactor
            * (length / diameter)
            * ((density * velocity * velocity) / 2.0)
        )

    static member BernoulliTotalPressure(staticPressure: Pressure, density: Density, velocity: Velocity) =
        Pressure.FromPascals(
            staticPressure.Pascals
            + (FluidDynamicsFunctions.DynamicPressure(density, velocity).Pascals)
        )

    static member MachNumber(velocity: Velocity, speedOfSound: Velocity) =
        let soundSpeed =
            ScientificGuard.positiveFinite "speedOfSound.MetersPerSecond" speedOfSound.MetersPerSecond

        velocity.MetersPerSecond / soundSpeed