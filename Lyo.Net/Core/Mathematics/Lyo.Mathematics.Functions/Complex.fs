namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models
open Lyo.Mathematics.Quantities

[<AbstractClass; Sealed>]
type ComplexFunctions private () =
    static member Conjugate(value: ComplexNumber) =
        ComplexNumber(value.Real, -value.Imaginary)

    static member Multiply(left: ComplexNumber, right: ComplexNumber) = left * right

    static member Divide(left: ComplexNumber, right: ComplexNumber) =
        let denominator = (right.Real * right.Real) + (right.Imaginary * right.Imaginary)

        if denominator = 0.0 then
            raise (DivideByZeroException())

        ComplexNumber(((left.Real * right.Real) + (left.Imaginary * right.Imaginary)) / denominator, ((left.Imaginary * right.Real) - (left.Real * right.Imaginary)) / denominator)

    static member ToPolar(value: ComplexNumber) =
        value.Magnitude, Angle.FromRadians(value.PhaseRadians)

    static member FromPolar(magnitude: double, phase: Angle) =
        ComplexNumber.FromPolar(magnitude, phase)