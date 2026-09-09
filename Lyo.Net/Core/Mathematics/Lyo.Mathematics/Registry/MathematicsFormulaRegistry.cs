namespace Lyo.Mathematics.Registry;

/// <summary>Hand-picked catalog of representative mathematics formulas implemented in <c>Lyo.Mathematics.Functions</c>.</summary>
/// <remarks>
/// <see cref="All" /> stays small on purpose — use it for discovery, documentation, or capability flags, not as an exhaustive API index. Prefer IDE metadata on the
/// concrete <c>*Functions</c> types for the full surface area.
/// </remarks>
public static class MathematicsFormulaRegistry
{
    /// <summary>Stable ids and metadata for selected production formulas (FFT, QR, integration, descriptive statistics, and similar).</summary>
    public static IReadOnlyList<FormulaDescriptor> All { get; } = [
        new("statistics.mean", "Statistics", "Lyo.Mathematics.Functions", "Mean", "Computes the arithmetic mean of a value set.", "StatisticsFunctions.Mean(double[])"),
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