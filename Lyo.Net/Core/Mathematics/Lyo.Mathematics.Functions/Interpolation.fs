namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type InterpolationFunctions private () =
    static member Linear(startValue: double, endValue: double, t: double) =
        let fraction = Guard.finite (nameof t) t
        startValue + ((endValue - startValue) * fraction)

    static member InverseLinear(startValue: double, endValue: double, value: double) =
        let denominator = Guard.nonZeroFinite "delta" (endValue - startValue)
        (Guard.finite (nameof value) value - startValue) / denominator

    static member Linear(input: InterpolationInput) =
        let denominator = Guard.nonZeroFinite "input.X1 - input.X0" (input.X1 - input.X0)
        input.Y0 + (((input.X - input.X0) / denominator) * (input.Y1 - input.Y0))

    static member PiecewiseLinear(xValues: double array, yValues: double array, x: double) =
        let xs = Guard.nonEmptyArray (nameof xValues) xValues
        let ys = Guard.nonEmptyArray (nameof yValues) yValues

        if xs.Length <> ys.Length then
            raise (ArgumentException("X and Y arrays must be the same length."))

        if xs.Length < 2 then
            raise (ArgumentException("At least two points are required."))

        let point = Guard.finite (nameof x) x
        let mutable index = 0

        while index < xs.Length - 2 && point > xs.[index + 1] do
            index <- index + 1

        InterpolationFunctions.Linear(InterpolationInput(xs.[index], ys.[index], xs.[index + 1], ys.[index + 1], point))