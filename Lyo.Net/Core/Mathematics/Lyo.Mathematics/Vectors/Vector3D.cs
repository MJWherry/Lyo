using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Vectors;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Vector3D(double x, double y, double z)
{
    public double X { get; } = MathValueGuards.Finite(x, nameof(x));

    public double Y { get; } = MathValueGuards.Finite(y, nameof(y));

    public double Z { get; } = MathValueGuards.Finite(z, nameof(z));

    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3D Normalize()
    {
        OperationHelpers.ThrowIf(Magnitude == 0, "Cannot normalize a zero vector.");
        return new(X / Magnitude, Y / Magnitude, Z / Magnitude);
    }

    public static double Dot(Vector3D left, Vector3D right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    public static double AngleBetween(Vector3D left, Vector3D right)
    {
        var denominator = left.Magnitude * right.Magnitude;
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot compute an angle with a zero vector.");
        var cosine = Clamp(Dot(left, right) / denominator, -1d, 1d);
        return Math.Acos(cosine);
    }

    public static Vector3D Project(Vector3D vector, Vector3D onto)
    {
        var denominator = Dot(onto, onto);
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot project onto a zero vector.");
        var scalar = Dot(vector, onto) / denominator;
        return onto * scalar;
    }

    public static Vector3D Cross(Vector3D left, Vector3D right)
        => new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);

    public static Vector3D operator +(Vector3D left, Vector3D right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3D operator -(Vector3D left, Vector3D right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3D operator *(Vector3D vector, double scalar) => new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}