namespace Lyo.Mathematics.Functions

open System

[<AbstractClass; Sealed>]
type SignalFunctions private () =
    static member Convolution(signal: double array, kernel: double array) =
        let source = Guard.nonEmptyArray (nameof signal) signal
        let filter = Guard.nonEmptyArray (nameof kernel) kernel
        source |> Array.iter (fun value -> Guard.finite (nameof signal) value |> ignore)
        filter |> Array.iter (fun value -> Guard.finite (nameof kernel) value |> ignore)

        Array.init (source.Length + filter.Length - 1) (fun index ->
            let mutable sum = 0.0

            for kernelIndex in 0 .. filter.Length - 1 do
                let signalIndex = index - kernelIndex

                if signalIndex >= 0 && signalIndex < source.Length then
                    sum <- sum + (source.[signalIndex] * filter.[kernelIndex])

            sum)

    static member MovingSum(values: double array, windowSize: int) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        if windowSize <= 0 || windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize))

        [| for i in 0 .. numbers.Length - windowSize -> numbers.[i .. i + windowSize - 1] |> Array.sum |]

    static member NormalizeMinMax(values: double array) =
        let numbers = Guard.nonEmptyArray (nameof values) values
        let minValue = Array.min numbers
        let maxValue = Array.max numbers

        if minValue = maxValue then
            Array.create numbers.Length 0.0
        else
            numbers |> Array.map (fun value -> (value - minValue) / (maxValue - minValue))