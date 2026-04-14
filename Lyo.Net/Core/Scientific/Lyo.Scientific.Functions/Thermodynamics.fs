namespace Lyo.Scientific.Functions

open System
open Lyo.Mathematics.Quantities
open Lyo.Scientific
open Lyo.Scientific.Engineering

[<AbstractClass; Sealed>]
type ThermodynamicsFunctions private () =
    static member HeatEnergy(input: HeatTransferInput) =
        let deltaT = input.FinalTemperature.Kelvin - input.InitialTemperature.Kelvin
        Energy.FromJoules(
            input.Mass.Kilograms
            * input.SpecificHeatCapacity.JoulesPerKilogramKelvin
            * deltaT
        )

    static member ConductionRate(input: ConductionInput) =
        let thickness =
            ScientificGuard.positiveFinite "input.Thickness.Meters" input.Thickness.Meters

        let deltaT = input.HotTemperature.Kelvin - input.ColdTemperature.Kelvin

        Power.FromWatts(
            input.ThermalConductivity.WattsPerMeterKelvin * input.Area.SquareMeters * deltaT
            / thickness
        )

    static member ThermalExpansion(input: ThermalExpansionInput) =
        let deltaT = input.FinalTemperature.Kelvin - input.InitialTemperature.Kelvin
        Length.FromMeters(input.InitialLength.Meters * (1.0 + (input.Coefficient.PerKelvin * deltaT)))

    static member CarnotEfficiency(hotReservoir: Temperature, coldReservoir: Temperature) =
        if coldReservoir.Kelvin >= hotReservoir.Kelvin then
            raise (ArgumentException("Hot reservoir temperature must be greater than cold reservoir temperature."))

        1.0 - (coldReservoir.Kelvin / hotReservoir.Kelvin)

    static member EntropyChange(heatTransferred: Energy, absoluteTemperature: Temperature) =
        Entropy.FromJoulesPerKelvin(heatTransferred.Joules / absoluteTemperature.Kelvin)

    static member InternalEnergyChange(moles: double, degreesOfFreedom: double, initialTemperature: Temperature, finalTemperature: Temperature) =
        let n = ScientificGuard.nonNegativeFinite (nameof moles) moles

        let dof =
            ScientificGuard.nonNegativeFinite (nameof degreesOfFreedom) degreesOfFreedom

        let deltaT = finalTemperature.Kelvin - initialTemperature.Kelvin
        Energy.FromJoules(0.5 * dof * n * ScientificConstants.GasConstant * deltaT)

    static member SpeedOfSoundIdealGas(temperature: Temperature, specificHeatRatio: double, molarMassKilogramsPerMole: double) =
        let gamma =
            ScientificGuard.positiveFinite (nameof specificHeatRatio) specificHeatRatio

        let molarMass =
            ScientificGuard.positiveFinite (nameof molarMassKilogramsPerMole) molarMassKilogramsPerMole

        Velocity.FromMetersPerSecond(Math.Sqrt(gamma * ScientificConstants.GasConstant * temperature.Kelvin / molarMass))

    static member ConvectiveHeatTransferRate(input: ConvectiveHeatTransferInput) =
        let deltaT = input.SurfaceTemperature.Kelvin - input.FluidTemperature.Kelvin

        Power.FromWatts(
            input.HeatTransferCoefficient.WattsPerSquareMeterKelvin
            * input.Area.SquareMeters
            * deltaT
        )

    static member RadiativeHeatTransferRate(input: RadiativeHeatTransferInput) =
        let sigma = 5.670374419e-8

        Power.FromWatts(
            input.Emissivity
            * sigma
            * input.Area.SquareMeters
            * (Math.Pow(input.HotTemperature.Kelvin, 4.0)
               - Math.Pow(input.ColdTemperature.Kelvin, 4.0))
        )

    static member HeatExchanger(input: HeatExchangerInput) =
        let hotCapacityRate =
            input.HotMassFlowRate.KilogramsPerSecond
            * input.HotSpecificHeatCapacity.JoulesPerKilogramKelvin

        let coldCapacityRate =
            input.ColdMassFlowRate.KilogramsPerSecond
            * input.ColdSpecificHeatCapacity.JoulesPerKilogramKelvin

        let capacityMin = Math.Min(hotCapacityRate, coldCapacityRate)

        let qMax =
            capacityMin
            * (input.HotInletTemperature.Kelvin - input.ColdInletTemperature.Kelvin)

        let q = input.Effectiveness * qMax
        let hotOut = input.HotInletTemperature.Kelvin - (q / hotCapacityRate)
        let coldOut = input.ColdInletTemperature.Kelvin + (q / coldCapacityRate)

        HeatExchangerResult(Energy.FromJoules(q), Temperature.FromKelvin(hotOut), Temperature.FromKelvin(coldOut))

    static member PrandtlNumber(specificHeatCapacity: SpecificHeatCapacity, dynamicViscosity: DynamicViscosity, thermalConductivity: ThermalConductivity) =
        let conductivity =
            ScientificGuard.positiveFinite "thermalConductivity.WattsPerMeterKelvin" thermalConductivity.WattsPerMeterKelvin

        (specificHeatCapacity.JoulesPerKilogramKelvin * dynamicViscosity.PascalSeconds)
        / conductivity

    static member GrashofNumber(input: NaturalConvectionInput) =
        let deltaT = input.SurfaceTemperature.Kelvin - input.FluidTemperature.Kelvin
        let length = input.CharacteristicLength.Meters

        let nu =
            ScientificGuard.positiveFinite "input.KinematicViscosity.SquareMetersPerSecond" input.KinematicViscosity.SquareMetersPerSecond

        input.Gravity.MetersPerSecondSquared
        * input.ThermalExpansionCoefficient.PerKelvin
        * deltaT
        * Math.Pow(length, 3.0)
        / (nu * nu)

    static member NusseltDittusBoelter(input: ConvectionCorrelationInput) =
        let exponent = if input.IsHeating then 0.4 else 0.3

        0.023
        * Math.Pow(input.ReynoldsNumber, 0.8)
        * Math.Pow(input.PrandtlNumber, exponent)

    static member HeatTransferCoefficientFromNusselt(input: ConvectionCorrelationInput) =
        let nu = ThermodynamicsFunctions.NusseltDittusBoelter(input)

        let length =
            ScientificGuard.positiveFinite "input.CharacteristicLength.Meters" input.CharacteristicLength.Meters

        HeatTransferCoefficient.FromWattsPerSquareMeterKelvin(nu * input.ThermalConductivity.WattsPerMeterKelvin / length)

    static member RadiationExchangeRate(input: RadiationExchangeInput) =
        let sigma = 5.670374419e-8

        let effectiveEmissivity =
            1.0 / ((1.0 / input.EmissivityHot) + (1.0 / input.EmissivityCold) - 1.0)

        Power.FromWatts(
            sigma
            * input.Area.SquareMeters
            * input.ViewFactor
            * effectiveEmissivity
            * (Math.Pow(input.HotTemperature.Kelvin, 4.0)
               - Math.Pow(input.ColdTemperature.Kelvin, 4.0))
        )