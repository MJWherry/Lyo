namespace Lyo.Scientific.Functions

open System
open Lyo.Mathematics.Quantities
open Lyo.Scientific.Engineering

[<AbstractClass; Sealed>]
type SolidMechanicsFunctions private () =
    static member NormalStress(input: StressStrainInput) =
        Pressure.FromPascals(input.Force.Newtons / input.CrossSectionArea.SquareMeters)

    static member NormalStrain(input: StressStrainInput) =
        input.LengthChange.Meters / input.OriginalLength.Meters

    static member YoungsModulus(input: StressStrainInput) =
        let strain = SolidMechanicsFunctions.NormalStrain(input)

        if strain = 0.0 then
            raise (DivideByZeroException())

        ModulusOfElasticity.FromPascals(SolidMechanicsFunctions.NormalStress(input).Pascals / strain)

    static member HookeExtension(force: Force, length: Length, area: Area, modulusOfElasticity: ModulusOfElasticity) =
        Length.FromMeters(
            force.Newtons * length.Meters
            / (area.SquareMeters * modulusOfElasticity.Pascals)
        )

    static member CantileverEndDeflection(input: BeamBendingInput) =
        let numerator = input.Force.Newtons * Math.Pow(input.BeamLength.Meters, 3.0)

        let denominator =
            3.0
            * input.ModulusOfElasticity.Pascals
            * input.AreaMomentOfInertia.MetersToFourth

        Length.FromMeters(numerator / denominator)

    static member BeamBendingStress(input: BeamBendingInput) =
        let moment = input.Force.Newtons * input.BeamLength.Meters

        Pressure.FromPascals(
            moment * input.DistanceFromNeutralAxis.Meters
            / input.AreaMomentOfInertia.MetersToFourth
        )

    static member CriticalFractureStress(input: FractureInput) =
        Pressure.FromPascals(
            input.FractureToughness.PascalRootMeters
            / Math.Sqrt(Math.PI * input.CrackLength.Meters)
        )

    static member FactorOfSafety(allowableStress: Pressure, appliedStress: Pressure) =
        let applied =
            ScientificGuard.positiveFinite "appliedStress.Pascals" appliedStress.Pascals

        allowableStress.Pascals / applied

    static member RectangularAreaMomentOfInertia(input: RectangularSectionInput) =
        AreaMomentOfInertia.FromMetersToFourth(input.Width.Meters * Math.Pow(input.Height.Meters, 3.0) / 12.0)

    static member CircularAreaMomentOfInertia(input: CircularSectionInput) =
        AreaMomentOfInertia.FromMetersToFourth(Math.PI * Math.Pow(input.Radius.Meters, 4.0) / 4.0)

    static member RectangularSectionModulus(input: RectangularSectionInput) =
        let i = SolidMechanicsFunctions.RectangularAreaMomentOfInertia(input)
        i.MetersToFourth / (input.Height.Meters / 2.0)

    static member GoodmanFactorOfSafety(input: FatigueInput) =
        1.0
        / ((input.AlternatingStress.Pascals / input.EnduranceLimit.Pascals)
           + (input.MeanStress.Pascals / input.UltimateStrength.Pascals))

    static member BeamBendingStress(force: Force, beamLength: Length, profile: BeamSectionProfile) =
        match box profile with
        | null -> raise (ArgumentNullException(nameof profile))
        | _ -> ()

        let moment = force.Newtons * beamLength.Meters

        Pressure.FromPascals(
            moment * profile.DistanceFromNeutralAxis.Meters
            / profile.AreaMomentOfInertia.MetersToFourth
        )

    static member FatigueLifeCycles(input: SNCurveInput) =
        0.5
        * Math.Pow(input.StressAmplitude.Pascals / input.FatigueStrengthCoefficient, 1.0 / input.FatigueStrengthExponent)

    static member FatigueDamageFraction(appliedCycles: double, cyclesToFailure: double) =
        let used = ScientificGuard.nonNegativeFinite (nameof appliedCycles) appliedCycles

        let capacity =
            ScientificGuard.positiveFinite (nameof cyclesToFailure) cyclesToFailure

        used / capacity