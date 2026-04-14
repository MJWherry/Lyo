namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models
open Lyo.Mathematics.Quantities

[<AbstractClass; Sealed>]
type TrigonometryFunctions private () =
    static member private Clamp(value: double, minimum: double, maximum: double) =
        if value < minimum then minimum
        elif value > maximum then maximum
        else value

    static member Sin(angle: Angle) = Math.Sin(angle.Radians)

    static member Cos(angle: Angle) = Math.Cos(angle.Radians)

    static member Tan(angle: Angle) = Math.Tan(angle.Radians)

    static member Asin(value: double) =
        Angle.FromRadians(Math.Asin(Guard.finite (nameof value) value))

    static member Acos(value: double) =
        Angle.FromRadians(Math.Acos(Guard.finite (nameof value) value))

    static member Atan(value: double) =
        Angle.FromRadians(Math.Atan(Guard.finite (nameof value) value))

    static member Sinh(value: double) =
        Math.Sinh(Guard.finite (nameof value) value)

    static member Cosh(value: double) =
        Math.Cosh(Guard.finite (nameof value) value)

    static member Tanh(value: double) =
        Math.Tanh(Guard.finite (nameof value) value)

    static member LawOfCosinesForSide(sideA: Length, sideB: Length, includedAngle: Angle) =
        let a = sideA.Meters
        let b = sideB.Meters
        let angle = includedAngle.Radians
        Length(Math.Sqrt((a * a) + (b * b) - (2.0 * a * b * Math.Cos(angle))))

    static member LawOfCosinesForAngle(input: TriangleInput) =
        let a = input.SideA.Meters
        let b = input.SideB.Meters
        let c = input.SideC.Meters
        let cosine = ((a * a) + (b * b) - (c * c)) / (2.0 * a * b)
        Angle.FromRadians(Math.Acos(TrigonometryFunctions.Clamp(cosine, -1.0, 1.0)))

    static member LawOfSinesForSide(knownSide: Length, knownAngle: Angle, targetAngle: Angle) =
        let ratio = knownSide.Meters / Math.Sin(knownAngle.Radians)
        Length(ratio * Math.Sin(targetAngle.Radians))