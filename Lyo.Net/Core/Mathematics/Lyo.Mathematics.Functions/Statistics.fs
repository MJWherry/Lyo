namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

module internal StatisticsInternals =
    let erf value =
        let sign = if value < 0.0 then -1.0 else 1.0
        let x = Math.Abs(value)
        let t = 1.0 / (1.0 + (0.3275911 * x))
        let a1 = 0.254829592
        let a2 = -0.284496736
        let a3 = 1.421413741
        let a4 = -1.453152027
        let a5 = 1.061405429
        let polynomial = (((((a5 * t) + a4) * t) + a3) * t + a2) * t + a1

        sign * (1.0 - (polynomial * t * Math.Exp(-x * x)))

    let inverseStandardNormal probability =
        let p = Guard.finite "probability" probability

        if p <= 0.0 || p >= 1.0 then
            raise (ArgumentOutOfRangeException("probability", "Probability must be between 0 and 1."))

        let a =
            [| -39.6968302866538
               220.946098424521
               -275.928510446969
               138.357751867269
               -30.6647980661472
               2.50662827745924 |]

        let b =
            [| -54.4760987982241
               161.585836858041
               -155.698979859887
               66.8013118877197
               -13.2806815528857 |]

        let c =
            [| -0.00778489400243029
               -0.322396458041136
               -2.40075827716184
               -2.54973253934373
               4.37466414146497
               2.93816398269878 |]

        let d =
            [| 0.00778469570904146; 0.32246712907004; 2.445134137143; 3.75440866190742 |]

        let plow = 0.02425
        let phigh = 1.0 - plow

        if p < plow then
            let q = Math.Sqrt(-2.0 * Math.Log(p))

            (((((c.[0] * q + c.[1]) * q + c.[2]) * q + c.[3]) * q + c.[4]) * q + c.[5])
            / ((((d.[0] * q + d.[1]) * q + d.[2]) * q + d.[3]) * q + 1.0)
        elif p <= phigh then
            let q = p - 0.5
            let r = q * q

            (((((a.[0] * r + a.[1]) * r + a.[2]) * r + a.[3]) * r + a.[4]) * r + a.[5]) * q
            / (((((b.[0] * r + b.[1]) * r + b.[2]) * r + b.[3]) * r + b.[4]) * r + 1.0)
        else
            let q = Math.Sqrt(-2.0 * Math.Log(1.0 - p))

            -((((((c.[0] * q + c.[1]) * q + c.[2]) * q + c.[3]) * q + c.[4]) * q + c.[5])
              / ((((d.[0] * q + d.[1]) * q + d.[2]) * q + d.[3]) * q + 1.0))

    let ranks (values: double array) =
        let indexed = values |> Array.indexed |> Array.sortBy snd
        let result = Array.zeroCreate<float> values.Length
        let mutable i = 0

        while i < indexed.Length do
            let mutable j = i

            while j + 1 < indexed.Length && (snd indexed.[j + 1]) = (snd indexed.[i]) do
                j <- j + 1

            let averageRank = ((float i) + (float j)) / 2.0 + 1.0

            for k in i..j do
                let originalIndex = indexed.[k] |> fst
                result.[originalIndex] <- averageRank

            i <- j + 1

        result

[<AbstractClass; Sealed>]
type StatisticsFunctions private () =
    static member Mode(values: double array) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        let grouped =
            numbers
            |> Array.groupBy id
            |> Array.map (fun (value, items) -> value, items.Length)

        let maxCount = grouped |> Array.maxBy snd |> snd
        grouped |> Array.filter (fun (_, count) -> count = maxCount) |> Array.map fst

    static member Range(values: double array) =
        let summary: DescriptiveStatisticsResult =
            StatisticsFunctions.Describe(values, false)

        summary.Maximum - summary.Minimum

    static member Mean(values: double array) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        Array.average numbers

    static member Median(values: double array) =
        let numbers = Guard.nonEmptyArray (nameof values) values |> Array.copy

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        Array.sortInPlace numbers

        if numbers.Length % 2 = 0 then
            let upper = numbers.Length / 2
            (numbers.[upper - 1] + numbers.[upper]) / 2.0
        else
            numbers.[numbers.Length / 2]

    static member Variance(values: double array, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if sample && numbers.Length < 2 then
            raise (ArgumentException("Sample variance requires at least two values.", nameof values))

        let mean = StatisticsFunctions.Mean(numbers)

        let squaredDifferences =
            numbers |> Array.sumBy (fun value -> let diff = value - mean in diff * diff)

        let divisor = if sample then numbers.Length - 1 else numbers.Length
        squaredDifferences / float divisor

    static member StandardDeviation(values: double array, sample: bool) =
        StatisticsFunctions.Variance(values, sample) |> Math.Sqrt

    static member Describe(values: double array, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values |> Array.copy

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        Array.sortInPlace numbers

        let mean = StatisticsFunctions.Mean(numbers)
        let median = StatisticsFunctions.Median(numbers)
        let variance = StatisticsFunctions.Variance(numbers, sample)
        let standardDeviation = Math.Sqrt(variance)
        let sum = Array.sum numbers

        DescriptiveStatisticsResult(mean, median, numbers.[0], numbers.[numbers.Length - 1], variance, standardDeviation, sum, numbers.Length)

    static member MovingAverage(values: double array, windowSize: int) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if windowSize <= 0 then
            raise (ArgumentOutOfRangeException(nameof windowSize, "Value must be greater than zero."))

        if windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize, "Window size cannot exceed the number of values."))

        [| for i in 0 .. (numbers.Length - windowSize) -> numbers.[i .. (i + windowSize - 1)] |> Array.average |]

    static member ExponentialMovingAverage(values: double array, smoothingFactor: double) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        let alpha = Guard.finite (nameof smoothingFactor) smoothingFactor

        if alpha <= 0.0 || alpha > 1.0 then
            raise (ArgumentOutOfRangeException(nameof smoothingFactor, "Smoothing factor must be in the range (0, 1]."))

        let result = Array.zeroCreate<double> numbers.Length
        result.[0] <- numbers.[0]

        for i in 1 .. numbers.Length - 1 do
            result.[i] <- (alpha * numbers.[i]) + ((1.0 - alpha) * result.[i - 1])

        result

    static member Percentile(values: double array, percentile: double) =
        let numbers = Guard.nonEmptyArray (nameof values) values |> Array.copy

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        let p = Guard.finite (nameof percentile) percentile

        if p < 0.0 || p > 100.0 then
            raise (ArgumentOutOfRangeException(nameof percentile, "Percentile must be between 0 and 100."))

        Array.sortInPlace numbers

        if numbers.Length = 1 then
            numbers.[0]
        else
            let position = (p / 100.0) * float (numbers.Length - 1)
            let lowerIndex = int (Math.Floor(position))
            let upperIndex = int (Math.Ceiling(position))

            if lowerIndex = upperIndex then
                numbers.[lowerIndex]
            else
                let weight = position - float lowerIndex
                (numbers.[lowerIndex] * (1.0 - weight)) + (numbers.[upperIndex] * weight)

    static member Quartiles(values: double array) =
        QuartilesResult(StatisticsFunctions.Percentile(values, 25.0), StatisticsFunctions.Percentile(values, 50.0), StatisticsFunctions.Percentile(values, 75.0))

    static member InterquartileRange(values: double array) =
        let quartiles = StatisticsFunctions.Quartiles(values)
        quartiles.Q3 - quartiles.Q1

    static member RollingMedian(values: double array, windowSize: int) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if windowSize <= 0 || windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize))

        [| for i in 0 .. (numbers.Length - windowSize) -> StatisticsFunctions.Median(numbers.[i .. (i + windowSize - 1)]) |]

    static member RollingMinimum(values: double array, windowSize: int) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if windowSize <= 0 || windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize))

        [| for i in 0 .. (numbers.Length - windowSize) -> numbers.[i .. (i + windowSize - 1)] |> Array.min |]

    static member RollingMaximum(values: double array, windowSize: int) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if windowSize <= 0 || windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize))

        [| for i in 0 .. (numbers.Length - windowSize) -> numbers.[i .. (i + windowSize - 1)] |> Array.max |]

    static member RollingStandardDeviation(values: double array, windowSize: int, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if windowSize <= 0 then
            raise (ArgumentOutOfRangeException(nameof windowSize, "Value must be greater than zero."))

        if windowSize > numbers.Length then
            raise (ArgumentOutOfRangeException(nameof windowSize, "Window size cannot exceed the number of values."))

        if sample && windowSize < 2 then
            raise (ArgumentOutOfRangeException(nameof windowSize, "Sample rolling standard deviation requires a window size of at least two."))

        [| for i in 0 .. (numbers.Length - windowSize) -> StatisticsFunctions.StandardDeviation(numbers.[i .. (i + windowSize - 1)], sample) |]

    static member ZScore(value: double, mean: double, standardDeviation: double) =
        let current = Guard.finite (nameof value) value
        let average = Guard.finite (nameof mean) mean
        let deviation = Guard.nonZeroFinite (nameof standardDeviation) standardDeviation
        (current - average) / deviation

    static member ZScores(values: double array, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        if sample && numbers.Length < 2 then
            raise (ArgumentException("Sample z-scores require at least two values.", nameof values))

        let summary = StatisticsFunctions.Describe(numbers, sample)

        if summary.StandardDeviation = 0.0 then
            Array.create numbers.Length 0.0
        else
            numbers
            |> Array.map (fun value -> StatisticsFunctions.ZScore(value, summary.Mean, summary.StandardDeviation))

    static member LatestZScore(values: double array, sample: bool) =
        let zScores = StatisticsFunctions.ZScores(values, sample)
        zScores.[zScores.Length - 1]

    static member IsAnomalyByZScore(values: double array, threshold: double, sample: bool) =
        let limit = Guard.positiveFinite (nameof threshold) threshold
        let latestZScore = StatisticsFunctions.LatestZScore(values, sample)
        Math.Abs(latestZScore) >= limit

    static member MedianAbsoluteDeviation(values: double array) =
        let numbers = Guard.nonEmptyArray (nameof values) values

        numbers
        |> Array.iter (fun value -> Guard.finite (nameof values) value |> ignore)

        let median = StatisticsFunctions.Median(numbers)

        numbers
        |> Array.map (fun value -> Math.Abs(value - median))
        |> StatisticsFunctions.Median

    static member IsAnomalyByMad(values: double array, threshold: double) =
        let limit = Guard.positiveFinite (nameof threshold) threshold
        let numbers = Guard.nonEmptyArray (nameof values) values
        let median = StatisticsFunctions.Median(numbers)
        let mad = StatisticsFunctions.MedianAbsoluteDeviation(numbers)

        if mad = 0.0 then
            false
        else
            let modifiedZ = 0.6745 * (Math.Abs(numbers.[numbers.Length - 1] - median) / mad)
            modifiedZ >= limit

    static member Skewness(values: double array, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values
        let summary = StatisticsFunctions.Describe(numbers, sample)

        if summary.StandardDeviation = 0.0 then
            0.0
        else
            let n = float numbers.Length

            let sumCubed =
                numbers
                |> Array.sumBy (fun value -> Math.Pow((value - summary.Mean) / summary.StandardDeviation, 3.0))

            if sample && numbers.Length > 2 then
                (n / ((n - 1.0) * (n - 2.0))) * sumCubed
            else
                sumCubed / n

    static member Kurtosis(values: double array, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values
        let summary = StatisticsFunctions.Describe(numbers, sample)

        if summary.StandardDeviation = 0.0 then
            0.0
        else
            let n = float numbers.Length

            let sumFourth =
                numbers
                |> Array.sumBy (fun value -> Math.Pow((value - summary.Mean) / summary.StandardDeviation, 4.0))

            if sample && numbers.Length > 3 then
                ((n * (n + 1.0)) / ((n - 1.0) * (n - 2.0) * (n - 3.0))) * sumFourth
                - ((3.0 * Math.Pow(n - 1.0, 2.0)) / ((n - 2.0) * (n - 3.0)))
            else
                (sumFourth / n) - 3.0

    static member Covariance(xValues: double array, yValues: double array, sample: bool) =
        let xs = Guard.nonEmptyArray "xValues" xValues
        let ys = Guard.nonEmptyArray "yValues" yValues

        if xs.Length <> ys.Length then
            raise (ArgumentException("X and Y arrays must be the same length."))

        if sample && xs.Length < 2 then
            raise (ArgumentException("Sample covariance requires at least two values."))

        let meanX = StatisticsFunctions.Mean(xs)
        let meanY = StatisticsFunctions.Mean(ys)

        let covarianceSum =
            Array.zip xs ys |> Array.sumBy (fun (x, y) -> (x - meanX) * (y - meanY))

        covarianceSum / float (if sample then xs.Length - 1 else xs.Length)

    static member PearsonCorrelation(xValues: double array, yValues: double array) =
        let covariance = StatisticsFunctions.Covariance(xValues, yValues, true)
        let stdX = StatisticsFunctions.StandardDeviation(xValues, true)
        let stdY = StatisticsFunctions.StandardDeviation(yValues, true)

        if stdX = 0.0 || stdY = 0.0 then
            0.0
        else
            covariance / (stdX * stdY)

    static member SpearmanCorrelation(xValues: double array, yValues: double array) =
        let xRanks = StatisticsInternals.ranks xValues
        let yRanks = StatisticsInternals.ranks yValues
        StatisticsFunctions.PearsonCorrelation(xRanks, yRanks)

    static member CovarianceCorrelation(xValues: double array, yValues: double array, sample: bool) =
        CovarianceCorrelationResult(StatisticsFunctions.Covariance(xValues, yValues, sample), StatisticsFunctions.PearsonCorrelation(xValues, yValues))

    static member WeightedMean(input: WeightedValuesInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        if input.Values.Length <> input.Weights.Length then
            raise (ArgumentException("Values and weights must be the same length.", nameof input))

        input.Values
        |> Array.iter (fun value -> Guard.finite "input.Values" value |> ignore)

        input.Weights
        |> Array.iter (fun weight -> Guard.nonNegativeFinite "input.Weights" weight |> ignore)

        let totalWeight = input.Weights |> Array.sum

        if totalWeight = 0.0 then
            raise (ArgumentException("Weights must sum to a positive value.", nameof input))

        Array.map2 (fun value weight -> value * weight) input.Values input.Weights
        |> Array.sum
        |> fun weightedTotal -> weightedTotal / totalWeight

    static member WeightedVariance(input: WeightedValuesInput) =
        let mean = StatisticsFunctions.WeightedMean(input)
        let totalWeight = input.Weights |> Array.sum

        Array.map2 (fun value weight -> weight * Math.Pow(value - mean, 2.0)) input.Values input.Weights
        |> Array.sum
        |> fun weightedSquared -> weightedSquared / totalWeight

    static member WeightedStatistics(input: WeightedValuesInput) =
        WeightedStatisticsResult(StatisticsFunctions.WeightedMean(input), StatisticsFunctions.WeightedVariance(input))

    static member MeanConfidenceInterval(values: double array, confidenceLevel: double, sample: bool) =
        let numbers = Guard.nonEmptyArray (nameof values) values
        let confidence = Guard.finite (nameof confidenceLevel) confidenceLevel

        if confidence <= 0.0 || confidence >= 1.0 then
            raise (ArgumentOutOfRangeException(nameof confidenceLevel, "Confidence level must be between 0 and 1."))

        let summary = StatisticsFunctions.Describe(numbers, sample)
        let zCritical = StatisticsInternals.inverseStandardNormal (0.5 + (confidence / 2.0))

        let margin =
            zCritical * (summary.StandardDeviation / Math.Sqrt(float summary.Count))

        ConfidenceIntervalResult(summary.Mean, margin, summary.Mean - margin, summary.Mean + margin, confidence)

    static member LinearRegression(input: LinearRegressionInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let xValues = Guard.nonEmptyArray "input.XValues" input.XValues
        let yValues = Guard.nonEmptyArray "input.YValues" input.YValues

        if xValues.Length <> yValues.Length then
            raise (ArgumentException("X and Y arrays must be the same length.", nameof input))

        if xValues.Length < 2 then
            raise (ArgumentException("Linear regression requires at least two points.", nameof input))

        xValues |> Array.iter (Guard.finite "input.XValues" >> ignore)
        yValues |> Array.iter (Guard.finite "input.YValues" >> ignore)

        let meanX = Array.average xValues
        let meanY = Array.average yValues

        let covariance =
            Array.zip xValues yValues
            |> Array.sumBy (fun (x, y) -> (x - meanX) * (y - meanY))

        let varianceX =
            xValues |> Array.sumBy (fun x -> let delta = x - meanX in delta * delta)

        let varianceY =
            yValues |> Array.sumBy (fun y -> let delta = y - meanY in delta * delta)

        if varianceX = 0.0 then
            raise (ArgumentException("X values must vary for regression.", nameof input))

        let slope = covariance / varianceX
        let intercept = meanY - (slope * meanX)

        let correlation =
            if varianceY = 0.0 then
                0.0
            else
                covariance / Math.Sqrt(varianceX * varianceY)

        LinearRegressionResult(slope, intercept, correlation)