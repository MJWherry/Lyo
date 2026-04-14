using System.Globalization;

namespace Lyo.Mathematics;

/// <summary>Internal helpers for concise, stable <see cref="object.ToString" /> output in mathematical domain types.</summary>
internal static class MathematicsDisplayFormat
{
    public static string DoubleArray(double[]? values, int maxPreview = 4)
    {
        if (values is null)
            return "null";

        if (values.Length == 0)
            return "n=0 []";

        var n = Math.Min(maxPreview, values.Length);
        var parts = new string[n];
        for (var i = 0; i < n; i++)
            parts[i] = values[i].ToString("0.###", CultureInfo.InvariantCulture);

        return values.Length > maxPreview ? $"n={values.Length} [{string.Join(", ", parts)}, …]" : $"n={values.Length} [{string.Join(", ", parts)}]";
    }

    public static string RectMatrix(double[,]? matrix)
    {
        if (matrix is null)
            return "null";

        return $"double[{matrix.GetLength(0)},{matrix.GetLength(1)}]";
    }

    public static string DelegateType(Delegate? del) => del?.GetType().Name ?? "null";
}