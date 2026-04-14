using Lyo.Mathematics.Models;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics;

public static class MathematicsWorkflowExtensions
{
    public static double[] ToMagnitudes(this ComplexNumber[] samples)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples));

        var magnitudes = new double[samples.Length];
        for (var i = 0; i < samples.Length; i++)
            magnitudes[i] = samples[i].Magnitude;

        return magnitudes;
    }

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