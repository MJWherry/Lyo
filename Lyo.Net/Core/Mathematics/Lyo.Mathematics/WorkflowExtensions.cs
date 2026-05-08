using Lyo.Exceptions;
using Lyo.Mathematics.Models;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics;

/// <summary>Small extension helpers for common mathematics workflows in C# call sites.</summary>
/// <remarks>Prefer static methods on <c>Lyo.Mathematics.Functions.*Functions</c> for substantive numerical work.</remarks>
public static class MathematicsWorkflowExtensions
{
    /// <summary>Projects each complex sample to its polar magnitude.</summary>
    /// <param name="samples">Complex samples (typically FFT bins or phasors).</param>
    /// <returns>Array of magnitudes with the same length as <paramref name="samples" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="samples" /> is <see langword="null" />.</exception>
    public static double[] ToMagnitudes(this ComplexNumber[] samples)
    {
        ArgumentHelpers.ThrowIfNull(samples);
        var magnitudes = new double[samples.Length];
        for (var i = 0; i < samples.Length; i++)
            magnitudes[i] = samples[i].Magnitude;

        return magnitudes;
    }

    /// <summary>Attempts to normalize <paramref name="vector" /> to unit length.</summary>
    /// <param name="vector">Input vector (may be zero).</param>
    /// <param name="normalized">Unit vector when <see langword="true" />; otherwise the zero vector.</param>
    /// <returns><see langword="false" /> when <paramref name="vector" /> has zero magnitude; otherwise <see langword="true" />.</returns>
    public static bool TryNormalize(this Vector3D vector, out Vector3D normalized)
    {
        if (vector.Magnitude == 0d) {
            normalized = new(0d, 0d, 0d);
            return false;
        }

        normalized = vector.Normalize();
        return true;
    }
}