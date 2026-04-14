using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Vectors;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Vector2D(double x, double y)
{
    public double X { get; } = MathValueGuards.Finite(x, nameof(x));

    public double Y { get; } = MathValueGuards.Finite(y, nameof(y));

    public double Magnitude => Math.Sqrt(X * X + Y * Y);

    public Vector2D Normalize()
    {
        OperationHelpers.ThrowIf(Magnitude == 0, "Cannot normalize a zero vector.");
        return new(X / Magnitude, Y / Magnitude);
    }

    public static double Dot(Vector2D left, Vector2D right) => left.X * right.X + left.Y * right.Y;

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    public static double AngleBetween(Vector2D left, Vector2D right)
    {
        var denominator = left.Magnitude * right.Magnitude;
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot compute an angle with a zero vector.");
        var cosine = Clamp(Dot(left, right) / denominator, -1d, 1d);
        return Math.Acos(cosine);
    }

    public static Vector2D Project(Vector2D vector, Vector2D onto)
    {
        var denominator = Dot(onto, onto);
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot project onto a zero vector.");
        var scalar = Dot(vector, onto) / denominator;
        return onto * scalar;
    }

    public static Vector2D operator +(Vector2D left, Vector2D right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector2D operator -(Vector2D left, Vector2D right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector2D operator *(Vector2D vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}