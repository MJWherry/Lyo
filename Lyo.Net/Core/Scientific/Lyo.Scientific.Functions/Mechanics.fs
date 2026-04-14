namespace Lyo.Scientific.Functions

open System
open Lyo.Mathematics.Quantities
open Lyo.Scientific
open Lyo.Scientific.Engineering

[<AbstractClass; Sealed>]
type MechanicsFunctions private () =
    static member MomentOfInertiaSolidCylinder(input: RotationalInertiaInput) =
        MomentOfInertia.FromKilogramSquareMeters(
            0.5
            * input.Mass.Kilograms
            * input.CharacteristicLength.Meters
            * input.CharacteristicLength.Meters
        )

    static member MomentOfInertiaSolidSphere(input: RotationalInertiaInput) =
        MomentOfInertia.FromKilogramSquareMeters(
            0.4
            * input.Mass.Kilograms
            * input.CharacteristicLength.Meters
            * input.CharacteristicLength.Meters
        )

    static member MomentOfInertiaRodAboutCenter(mass: Mass, length: Length) =
        MomentOfInertia.FromKilogramSquareMeters((1.0 / 12.0) * mass.Kilograms * length.Meters * length.Meters)

    static member RotationalKineticEnergy(input: RotationalEnergyInput) =
        Energy.FromJoules(
            0.5
            * input.MomentOfInertia.KilogramSquareMeters
            * input.AngularVelocity.RadiansPerSecond
            * input.AngularVelocity.RadiansPerSecond
        )

    static member AngularMomentum(input: AngularMomentumInput) =
        AngularMomentum.FromKilogramSquareMetersPerSecond(
            input.MomentOfInertia.KilogramSquareMeters
            * input.AngularVelocity.RadiansPerSecond
        )

    static member SpringOscillationPeriod(input: SpringOscillatorInput) =
        TimeInterval.FromSeconds(
            2.0
            * ScientificConstants.Pi
            * Math.Sqrt(input.Mass.Kilograms / input.SpringConstant.NewtonsPerMeter)
        )

    static member PendulumPeriod(input: PendulumInput) =
        let gravity =
            ScientificGuard.positiveFinite "input.Gravity.MetersPerSecondSquared" input.Gravity.MetersPerSecondSquared

        TimeInterval.FromSeconds(2.0 * ScientificConstants.Pi * Math.Sqrt(input.Length.Meters / gravity))

    static member MechanicalAdvantage(outputForce: Force, inputForce: Force) =
        let inputValue =
            ScientificGuard.positiveFinite "inputForce.Newtons" inputForce.Newtons

        outputForce.Newtons / inputValue