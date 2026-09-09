namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

module internal TransformInternals =
    let complexScale (scalar: double) (value: ComplexNumber) =
        ComplexNumber(value.Real * scalar, value.Imaginary * scalar)

    let complexExp (angle: double) =
        ComplexNumber(Math.Cos(angle), Math.Sin(angle))

    let rec fft (values: ComplexNumber array) invert =
        if values.Length = 1 then
            [| values[0] |]
        elif values.Length % 2 <> 0 then
            raise (ArgumentException("FFT input length must be a power of two.", "values"))
        else
            let evens =
                values
                |> Array.mapi (fun i value -> i, value)
                |> Array.choose (fun (i, value) -> if i % 2 = 0 then Some value else None)

            let odds =
                values
                |> Array.mapi (fun i value -> i, value)
                |> Array.choose (fun (i, value) -> if i % 2 = 1 then Some value else None)

            let evenResult = fft evens invert
            let oddResult = fft odds invert
            let result = Array.zeroCreate<ComplexNumber> values.Length
            let sign = if invert then 1.0 else -1.0

            for k in 0 .. (values.Length / 2) - 1 do
                let twiddle =
                    complexExp (sign * 2.0 * Math.PI * float k / float values.Length) * oddResult[k]

                result[k] <- evenResult[k] + twiddle
                result[k + (values.Length / 2)] <- evenResult[k] - twiddle

            if invert then
                result |> Array.map (complexScale 0.5)
            else
                result

[<AbstractClass; Sealed>]
type TransformFunctions private () =
    static member FastFourierTransform(values: ComplexNumber array) =
        match box values with
        | null -> raise (ArgumentNullException(nameof values))
        | _ when values.Length = 0 -> raise (ArgumentException("Array must contain at least one value.", nameof values))
        | _ -> TransformInternals.fft values false

    static member InverseFastFourierTransform(values: ComplexNumber array) =
        match box values with
        | null -> raise (ArgumentNullException(nameof values))
        | _ when values.Length = 0 -> raise (ArgumentException("Array must contain at least one value.", nameof values))
        | _ -> TransformInternals.fft values true