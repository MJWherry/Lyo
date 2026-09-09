using Lyo.Exceptions;

namespace Lyo.Mathematics;

internal static class MathValueGuards
{
    public static double Finite(double value, string paramName)
    {
        ArgumentHelpers.ThrowIf(double.IsNaN(value) || double.IsInfinity(value), "Value must be a finite number.", paramName);
        return value;
    }

    public static double NonNegativeFinite(double value, string paramName)
    {
        var finite = Finite(value, paramName);
        ArgumentHelpers.ThrowIfNegative(finite, paramName);
        return finite;
    }

    public static double PositiveFinite(double value, string paramName)
    {
        var finite = Finite(value, paramName);
        ArgumentHelpers.ThrowIfNegativeOrZero(finite, paramName);
        return finite;
    }
}