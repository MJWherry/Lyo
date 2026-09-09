namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type OptimizationFunctions private () =
    static member GradientDescent(input: GradientDescentInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let mutable current = input.InitialGuess

        for _ in 1 .. input.Iterations do
            current <- current - (input.LearningRate * input.Derivative.Invoke(current))

        OptimizationResult(current, input.Iterations)