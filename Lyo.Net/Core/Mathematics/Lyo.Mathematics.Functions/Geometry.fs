namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models
open Lyo.Mathematics.Quantities
open Lyo.Mathematics.Vectors

[<AbstractClass; Sealed>]
type GeometryFunctions private () =
    static member CircleArea(input: CircleMeasurementInput) =
        let radius = Guard.nonNegativeFinite "input.Radius.Meters" input.Radius.Meters
        Area(Math.PI * radius * radius)

    static member CircleCircumference(input: CircleMeasurementInput) =
        let radius = Guard.nonNegativeFinite "input.Radius.Meters" input.Radius.Meters
        Length(2.0 * Math.PI * radius)

    static member RectangleArea(input: RectangleMeasurementInput) =
        Area(input.Width.Meters * input.Height.Meters)

    static member RectanglePerimeter(input: RectangleMeasurementInput) =
        Length(2.0 * (input.Width.Meters + input.Height.Meters))

    static member RectangleDiagonal(input: RectangleMeasurementInput) =
        Length(
            Math.Sqrt(
                (input.Width.Meters * input.Width.Meters)
                + (input.Height.Meters * input.Height.Meters)
            )
        )

    static member RightTriangleHypotenuse(input: RightTriangleInput) =
        Length(
            Math.Sqrt(
                (input.SideA.Meters * input.SideA.Meters)
                + (input.SideB.Meters * input.SideB.Meters)
            )
        )

    static member DistanceBetween(startPoint: Vector2D, endPoint: Vector2D) =
        let deltaX = endPoint.X - startPoint.X
        let deltaY = endPoint.Y - startPoint.Y
        Length(Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)))

    static member DegreesToRadians(degrees: double) = Angle.FromDegrees(degrees).Radians

    static member RadiansToDegrees(radians: double) = Angle.FromRadians(radians).Degrees