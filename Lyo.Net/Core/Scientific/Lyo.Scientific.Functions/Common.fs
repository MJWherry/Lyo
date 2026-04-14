namespace Lyo.Scientific.Functions

open System

module internal ScientificGuard =
    let nonEmptyString (name: string) (value: string) =
        if String.IsNullOrWhiteSpace(value) then
            raise (ArgumentException("Value cannot be null or whitespace.", name))

        value

    let finite (name: string) (value: double) =
        if Double.IsNaN(value) || Double.IsInfinity(value) then
            raise (ArgumentOutOfRangeException(name, "Value must be finite."))

        value

    let nonNegativeFinite (name: string) (value: double) =
        let finiteValue = finite name value

        if finiteValue < 0.0 then
            raise (ArgumentOutOfRangeException(name, "Value must be non-negative."))

        finiteValue

    let positiveFinite (name: string) (value: double) =
        let finiteValue = finite name value

        if finiteValue <= 0.0 then
            raise (ArgumentOutOfRangeException(name, "Value must be positive."))

        finiteValue

    let nonEmptyArray<'T> (name: string) (values: 'T array) =
        match box values with
        | null -> raise (ArgumentNullException(name))
        | _ when values.Length = 0 -> raise (ArgumentException("Array must contain at least one value.", name))
        | _ -> values