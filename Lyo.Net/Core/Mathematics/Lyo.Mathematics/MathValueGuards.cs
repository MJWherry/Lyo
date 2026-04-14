namespace Lyo.Mathematics;

internal static class MathValueGuards
{
    public static double Finite(double value, string paramName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");

        return value;
    }

    public static double NonNegativeFinite(double value, string paramName)
    {
        var finite = Finite(value, paramName);
        if (finite < 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than or equal to zero.");

        return finite;
    }

    public static double PositiveFinite(double value, string paramName)
    {
        var finite = Finite(value, paramName);
        if (finite <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");

        return finite;
    }
}