namespace Lyo.Mathematics.Registry;

public static class MathematicsFormulaRegistry
{
    public static IReadOnlyList<FormulaDescriptor> All { get; } = [
        new("arithmetic.average", "Arithmetic", "Lyo.Mathematics.Functions", "Average", "Computes the arithmetic mean of a value set.", "ArithmeticFunctions.Average(double[])"),
        new(
            "statistics.describe", "Statistics", "Lyo.Mathematics.Functions", "Describe", "Summarizes count, mean, spread, min, and max.",
            "StatisticsFunctions.Describe(double[])"),
        new(
            "trigonometry.law_of_cosines", "Trigonometry", "Lyo.Mathematics.Functions", "Law Of Cosines", "Solves triangle sides and angles from known geometry.",
            "TrigonometryFunctions.LawOfCosinesForSide(...)"),
        new(
            "calculus.adaptive_integration", "Calculus", "Lyo.Mathematics.Functions", "Adaptive Integration", "Integrates with adaptive Simpson refinement.",
            "CalculusFunctions.AdaptiveIntegration(AdaptiveIntegrationInput)"),
        new(
            "linear_algebra.qr", "Linear Algebra", "Lyo.Mathematics.Functions", "QR Decomposition", "Factorizes a full-rank matrix into orthonormal and upper-triangular factors.",
            "LinearAlgebraFunctions.QrDecomposition(double[,])"),
        new(
            "signal.fft", "Signal Processing", "Lyo.Mathematics.Functions", "Fast Fourier Transform", "Transforms power-of-two complex samples into the frequency domain.",
            "TransformFunctions.FastFourierTransform(ComplexNumber[])")
    ];
}