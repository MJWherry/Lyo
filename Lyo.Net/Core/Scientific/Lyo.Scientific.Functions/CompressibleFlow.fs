namespace Lyo.Scientific.Functions

open System
open Lyo.Mathematics.Quantities
open Lyo.Scientific.Engineering

[<AbstractClass; Sealed>]
type CompressibleFlowFunctions private () =
    static member private NormalShockPressureRatio(gamma: double, machNormal: double) =
        1.0 + ((2.0 * gamma) / (gamma + 1.0)) * ((machNormal * machNormal) - 1.0)

    static member StaticTemperature(input: CompressibleFlowInput) =
        Temperature.FromKelvin(
            input.StagnationTemperature.Kelvin
            / (1.0
               + (((input.SpecificHeatRatio - 1.0) / 2.0) * input.MachNumber * input.MachNumber))
        )

    static member StaticPressure(input: CompressibleFlowInput) =
        let ratio =
            1.0
            + (((input.SpecificHeatRatio - 1.0) / 2.0) * input.MachNumber * input.MachNumber)

        Pressure.FromPascals(
            input.StagnationPressure.Pascals
            / Math.Pow(ratio, input.SpecificHeatRatio / (input.SpecificHeatRatio - 1.0))
        )

    static member ChokedMassFlowRate(input: NozzleFlowInput, throatArea: Area) =
        let gamma = input.SpecificHeatRatio

        let factor =
            Math.Sqrt(gamma / input.GasConstant)
            * Math.Pow(2.0 / (gamma + 1.0), (gamma + 1.0) / (2.0 * (gamma - 1.0)))

        MassFlowRate.FromKilogramsPerSecond(
            throatArea.SquareMeters * input.ChamberPressure.Pascals
            / Math.Sqrt(input.ChamberTemperature.Kelvin)
            * factor
        )

    static member NozzleExitVelocity(input: NozzleFlowInput) =
        let gamma = input.SpecificHeatRatio
        let pressureRatio = input.ExitPressure.Pascals / input.ChamberPressure.Pascals

        let value =
            ((2.0 * gamma) / (gamma - 1.0))
            * input.GasConstant
            * input.ChamberTemperature.Kelvin
            * (1.0 - Math.Pow(pressureRatio, (gamma - 1.0) / gamma))

        Velocity.FromMetersPerSecond(Math.Sqrt(value))

    static member NozzleFlow(input: NozzleFlowInput, throatArea: Area) =
        NozzleFlowResult(CompressibleFlowFunctions.NozzleExitVelocity(input), CompressibleFlowFunctions.ChokedMassFlowRate(input, throatArea))

    static member IsentropicAreaMachRatio(specificHeatRatio: double, machNumber: double) =
        let gamma =
            ScientificGuard.positiveFinite (nameof specificHeatRatio) specificHeatRatio

        let mach = ScientificGuard.positiveFinite (nameof machNumber) machNumber
        let term = (2.0 / (gamma + 1.0)) * (1.0 + (((gamma - 1.0) / 2.0) * mach * mach))
        (1.0 / mach) * Math.Pow(term, (gamma + 1.0) / (2.0 * (gamma - 1.0)))

    static member SolveMachFromAreaRatio(areaRatio: double, specificHeatRatio: double, supersonicBranch: bool) =
        let target = ScientificGuard.positiveFinite (nameof areaRatio) areaRatio

        let gamma =
            ScientificGuard.positiveFinite (nameof specificHeatRatio) specificHeatRatio

        let mutable lower = if supersonicBranch then 1.0001 else 0.01
        let mutable upper = if supersonicBranch then 20.0 else 0.9999

        for _ in 1..100 do
            let midpoint = (lower + upper) / 2.0
            let value = CompressibleFlowFunctions.IsentropicAreaMachRatio(gamma, midpoint)

            if value > target then
                if supersonicBranch then
                    upper <- midpoint
                else
                    lower <- midpoint
            else if supersonicBranch then
                lower <- midpoint
            else
                upper <- midpoint

        (lower + upper) / 2.0

    static member DownstreamMachNormalShock(input: NormalShockInput) =
        let gamma = input.SpecificHeatRatio
        let m1 = input.UpstreamMachNumber

        Math.Sqrt(
            (1.0 + (((gamma - 1.0) / 2.0) * m1 * m1))
            / ((gamma * m1 * m1) - ((gamma - 1.0) / 2.0))
        )

    static member PressureRatioNormalShock(input: NormalShockInput) =
        CompressibleFlowFunctions.NormalShockPressureRatio(input.SpecificHeatRatio, input.UpstreamMachNumber)

    static member TemperatureRatioNormalShock(input: NormalShockInput) =
        let gamma = input.SpecificHeatRatio
        let m1 = input.UpstreamMachNumber
        let pressureRatio = CompressibleFlowFunctions.PressureRatioNormalShock(input)
        let densityRatio = ((gamma + 1.0) * m1 * m1) / (((gamma - 1.0) * m1 * m1) + 2.0)
        pressureRatio / densityRatio

    static member ObliqueShock(input: ObliqueShockInput) =
        let gamma = input.SpecificHeatRatio
        let beta = input.ShockAngle.Radians
        let m1 = input.UpstreamMachNumber
        let normalMachBefore = m1 * Math.Sin(beta)

        let normalMachAfter =
            CompressibleFlowFunctions.DownstreamMachNormalShock(NormalShockInput(normalMachBefore, gamma))

        let numerator =
            2.0
            * (1.0 / Math.Tan(beta))
            * ((m1 * m1 * Math.Sin(beta) * Math.Sin(beta)) - 1.0)

        let denominator = (m1 * m1 * (gamma + Math.Cos(2.0 * beta))) + 2.0
        let theta = Math.Atan(numerator / denominator)

        let pressureRatio =
            CompressibleFlowFunctions.NormalShockPressureRatio(gamma, normalMachBefore)

        ObliqueShockResult(normalMachBefore, normalMachAfter, pressureRatio, Angle.FromRadians(theta))