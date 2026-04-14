namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type AlgebraFunctions private () =
    static member EvaluatePolynomial(coefficients: double array, x: double) =
        let coeffs = Guard.nonEmptyArray (nameof coefficients) coefficients

        coeffs
        |> Array.iter (fun value -> Guard.finite (nameof coefficients) value |> ignore)

        let point = Guard.finite (nameof x) x
        coeffs |> Array.fold (fun acc coefficient -> (acc * point) + coefficient) 0.0

    static member EvaluatePolynomial(input: PolynomialInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        AlgebraFunctions.EvaluatePolynomial(input.Coefficients, input.X)

    static member EvaluatePolynomialDerivative(coefficients: double array, x: double) =
        let coeffs = Guard.nonEmptyArray (nameof coefficients) coefficients

        coeffs
        |> Array.iter (fun value -> Guard.finite (nameof coefficients) value |> ignore)

        let point = Guard.finite (nameof x) x

        if coeffs.Length = 1 then
            0.0
        else
            coeffs.[0 .. coeffs.Length - 2]
            |> Array.mapi (fun index coefficient -> coefficient * float (coeffs.Length - index - 1))
            |> Array.fold (fun acc coefficient -> (acc * point) + coefficient) 0.0

    static member SolveLinear(a: double, b: double) =
        let coefficient = Guard.nonZeroFinite (nameof a) a
        let intercept = Guard.finite (nameof b) b
        -intercept / coefficient

    static member SolveQuadratic(input: QuadraticEquationInput) =
        let a = Guard.nonZeroFinite "input.A" input.A
        let b = Guard.finite "input.B" input.B
        let c = Guard.finite "input.C" input.C
        let discriminant = (b * b) - (4.0 * a * c)

        if discriminant < 0.0 then
            QuadraticEquationResult(discriminant, Nullable<double>(), Nullable<double>(), false)
        else
            let root = Math.Sqrt(discriminant)
            let denominator = 2.0 * a
            let root1 = (-b + root) / denominator
            let root2 = (-b - root) / denominator
            QuadraticEquationResult(discriminant, Nullable<double>(root1), Nullable<double>(root2), true)