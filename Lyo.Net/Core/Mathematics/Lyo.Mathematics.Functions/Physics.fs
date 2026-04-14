namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models
open Lyo.Mathematics.Quantities
open Lyo.Scientific

[<AbstractClass; Sealed>]
type PhysicsFunctions private () =
    static member Momentum(input: MomentumInput) =
        Momentum(input.Mass.Kilograms * input.Velocity.MetersPerSecond)

    static member Force(input: ForceInput) =
        Force(input.Mass.Kilograms * input.Acceleration.MetersPerSecondSquared)

    static member KineticEnergy(input: KineticEnergyInput) =
        let speed = input.Velocity.MetersPerSecond
        Energy(0.5 * input.Mass.Kilograms * speed * speed)

    static member AverageVelocity(input: AverageVelocityInput) =
        let seconds =
            Guard.positiveFinite "input.ElapsedTime.Seconds" input.ElapsedTime.Seconds
        Velocity(input.Distance.Meters / seconds)

    static member Work(force: Force, distance: Length) = Energy(force.Newtons * distance.Meters)

    static member FinalVelocity(initialVelocity: Velocity, acceleration: Acceleration, elapsedTime: TimeInterval) =
        Velocity(
            initialVelocity.MetersPerSecond
            + (acceleration.MetersPerSecondSquared * elapsedTime.Seconds)
        )

    static member Displacement(initialVelocity: Velocity, elapsedTime: TimeInterval, acceleration: Acceleration) =
        Length(
            (initialVelocity.MetersPerSecond * elapsedTime.Seconds)
            + (0.5
               * acceleration.MetersPerSecondSquared
               * elapsedTime.Seconds
               * elapsedTime.Seconds)
        )

    static member ProjectileMotion(input: ProjectileMotionInput) =
        let speed = input.InitialVelocity.MetersPerSecond
        let theta = input.LaunchAngle.Radians

        let gravity =
            Guard.positiveFinite "input.Gravity.MetersPerSecondSquared" (Math.Abs(input.Gravity.MetersPerSecondSquared))

        let initialHeight = input.InitialHeight.Meters
        let vx = speed * Math.Cos(theta)
        let vy = speed * Math.Sin(theta)
        let discriminant = (vy * vy) + (2.0 * gravity * initialHeight)
        let flightTime = (vy + Math.Sqrt(discriminant)) / gravity
        let maxHeight = initialHeight + ((vy * vy) / (2.0 * gravity))
        let finalVerticalVelocity = vy - (gravity * flightTime)

        ProjectileMotionResult(TimeInterval(flightTime), Length(vx * flightTime), Length(maxHeight), Velocity(vx), Velocity(finalVerticalVelocity))

    static member AngularVelocity(input: AngularMotionInput) =
        let seconds =
            Guard.positiveFinite "input.ElapsedTime.Seconds" input.ElapsedTime.Seconds

        AngularVelocity(input.AngularDisplacement.Radians / seconds)

    static member AngularAcceleration(initialVelocity: AngularVelocity, finalVelocity: AngularVelocity, elapsedTime: TimeInterval) =
        let seconds = Guard.positiveFinite (nameof elapsedTime) elapsedTime.Seconds
        AngularAcceleration((finalVelocity.RadiansPerSecond - initialVelocity.RadiansPerSecond) / seconds)

    static member Torque(input: TorqueInput) =
        Torque(
            input.LeverArm.Meters
            * input.Force.Newtons
            * Math.Sin(input.AngleBetween.Radians)
        )

    static member Power(input: PowerInput) =
        let seconds =
            Guard.positiveFinite "input.ElapsedTime.Seconds" input.ElapsedTime.Seconds

        Power(input.Work.Joules / seconds)

    static member Impulse(input: ImpulseInput) =
        Momentum(input.AverageForce.Newtons * input.ContactTime.Seconds)

    static member ElasticCollision1D(input: Collision1DInput) =
        let m1 = input.Mass1.Kilograms
        let m2 = input.Mass2.Kilograms
        let v1 = input.Velocity1.MetersPerSecond
        let v2 = input.Velocity2.MetersPerSecond
        let totalBefore = Momentum((m1 * v1) + (m2 * v2))
        let denominator = Guard.positiveFinite "totalMass" (m1 + m2)

        let finalVelocity1 =
            (((m1 - m2) / denominator) * v1) + (((2.0 * m2) / denominator) * v2)

        let finalVelocity2 =
            (((2.0 * m1) / denominator) * v1) + (((m2 - m1) / denominator) * v2)

        let totalAfter = Momentum((m1 * finalVelocity1) + (m2 * finalVelocity2))

        Collision1DResult(Velocity(finalVelocity1), Velocity(finalVelocity2), totalBefore, totalAfter)

    static member PerfectlyInelasticCollision1D(input: Collision1DInput) =
        let m1 = input.Mass1.Kilograms
        let m2 = input.Mass2.Kilograms
        let v1 = input.Velocity1.MetersPerSecond
        let v2 = input.Velocity2.MetersPerSecond
        let totalBefore = Momentum((m1 * v1) + (m2 * v2))
        let sharedVelocity = totalBefore.KilogramMetersPerSecond / (m1 + m2)
        let totalAfter = Momentum((m1 + m2) * sharedVelocity)

        Collision1DResult(Velocity(sharedVelocity), Velocity(sharedVelocity), totalBefore, totalAfter)

    static member GravitationalForce(input: GravitationalForceInput) =
        let distance =
            Guard.positiveFinite "input.DistanceBetweenCenters.Meters" input.DistanceBetweenCenters.Meters

        Force(
            ScientificConstants.GravitationalConstant
            * input.Mass1.Kilograms
            * input.Mass2.Kilograms
            / (distance * distance)
        )

    static member GravitationalPotentialEnergy(mass1: Mass, mass2: Mass, distanceBetweenCenters: Length) =
        let distance =
            Guard.positiveFinite (nameof distanceBetweenCenters) distanceBetweenCenters.Meters

        Energy(
            -(ScientificConstants.GravitationalConstant * mass1.Kilograms * mass2.Kilograms
              / distance)
        )

    static member SpringForce(input: SpringForceInput) =
        Force(-(input.SpringConstant.NewtonsPerMeter * input.Displacement.Meters))

    static member SpringPotentialEnergy(input: SpringForceInput) =
        Energy(
            0.5
            * input.SpringConstant.NewtonsPerMeter
            * input.Displacement.Meters
            * input.Displacement.Meters
        )

    static member Pressure(input: PressureInput) =
        let area = Guard.positiveFinite "input.Area.SquareMeters" input.Area.SquareMeters
        Pressure(input.Force.Newtons / area)

    static member Density(input: DensityInput) =
        let volume =
            Guard.positiveFinite "input.Volume.CubicMeters" input.Volume.CubicMeters

        Density(input.Mass.Kilograms / volume)

    static member WaveSpeed(input: WaveInput) =
        Velocity(input.Frequency.Hertz * input.Wavelength.Meters)

    static member FrequencyFromWaveSpeed(speed: Velocity, wavelength: Length) =
        let lambda = Guard.positiveFinite (nameof wavelength) wavelength.Meters
        Frequency(speed.MetersPerSecond / lambda)

    static member WavelengthFromWaveSpeed(speed: Velocity, frequency: Frequency) =
        let hz = Guard.positiveFinite (nameof frequency) frequency.Hertz
        Length(speed.MetersPerSecond / hz)

    static member Voltage(current: ElectricCurrent, resistance: Resistance) =
        Voltage(current.Amperes * resistance.Ohms)

    static member Current(voltage: Voltage, resistance: Resistance) =
        let ohms = Guard.positiveFinite (nameof resistance) resistance.Ohms
        ElectricCurrent(voltage.Volts / ohms)

    static member Resistance(voltage: Voltage, current: ElectricCurrent) =
        let amperes = Guard.nonZeroFinite (nameof current) current.Amperes
        Resistance(voltage.Volts / amperes)

    static member ElectricPower(voltage: Voltage, current: ElectricCurrent) = Power(voltage.Volts * current.Amperes)

    static member ElectricPower(voltage: Voltage, resistance: Resistance) =
        let ohms = Guard.positiveFinite (nameof resistance) resistance.Ohms
        Power((voltage.Volts * voltage.Volts) / ohms)

    static member SeriesResistance(resistances: Resistance array) =
        let values = Guard.nonEmptyArray (nameof resistances) resistances
        Resistance(values |> Array.sumBy (fun resistance -> resistance.Ohms))

    static member ParallelResistance(resistances: Resistance array) =
        let values = Guard.nonEmptyArray (nameof resistances) resistances

        let inverseTotal =
            values
            |> Array.sumBy (fun resistance ->
                let ohms = Guard.positiveFinite "resistance.Ohms" resistance.Ohms
                1.0 / ohms)

        Resistance(1.0 / inverseTotal)

    static member SeriesCapacitance(capacitances: Capacitance array) =
        let values = Guard.nonEmptyArray (nameof capacitances) capacitances

        let inverseTotal =
            values
            |> Array.sumBy (fun capacitance ->
                let farads = Guard.positiveFinite "capacitance.Farads" capacitance.Farads
                1.0 / farads)

        Capacitance(1.0 / inverseTotal)

    static member ParallelCapacitance(capacitances: Capacitance array) =
        let values = Guard.nonEmptyArray (nameof capacitances) capacitances
        Capacitance(values |> Array.sumBy (fun capacitance -> capacitance.Farads))

    static member IdealGasPressure(input: IdealGasLawInput) =
        Pressure(
            input.Moles * ScientificConstants.GasConstant * input.Temperature.Kelvin
            / input.Volume.CubicMeters
        )

    static member IdealGasVolume(input: IdealGasLawInput) =
        Volume(
            input.Moles * ScientificConstants.GasConstant * input.Temperature.Kelvin
            / input.Pressure.Pascals
        )

    static member IdealGasTemperature(input: IdealGasLawInput) =
        Temperature(
            input.Pressure.Pascals * input.Volume.CubicMeters
            / (input.Moles * ScientificConstants.GasConstant)
        )

    static member BodyMassIndex(input: BodyMassIndexInput) =
        let height = Guard.positiveFinite "input.Height.Meters" input.Height.Meters
        input.Mass.Kilograms / (height * height)