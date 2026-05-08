using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Vectors;

/// <summary>Three-dimensional Euclidean vector with finite components.</summary>
/// <remarks>Provides dot/cross products, angles, projections, and normalization consistent with graphics and mechanics conventions.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Vector3D
{
    /// <summary>X component.</summary>
    public double X { get; }

    /// <summary>Y component.</summary>
    public double Y { get; }

    /// <summary>Z component.</summary>
    public double Z { get; }

    /// <summary>Euclidean length √(x² + y² + z²).</summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Creates a vector after validating finite components.</summary>
    /// <param name="x">Finite X component.</param>
    /// <param name="y">Finite Y component.</param>
    /// <param name="z">Finite Z component.</param>
    public Vector3D(double x, double y, double z)
    {
        X = MathValueGuards.Finite(x, nameof(x));
        Y = MathValueGuards.Finite(y, nameof(y));
        Z = MathValueGuards.Finite(z, nameof(z));
    }

    /// <summary>Returns a unit vector in the same direction.</summary>
    /// <exception cref="InvalidOperationException">The vector has zero magnitude.</exception>
    public Vector3D Normalize()
    {
        OperationHelpers.ThrowIf(Magnitude == 0, "Cannot normalize a zero vector.");
        return new(X / Magnitude, Y / Magnitude, Z / Magnitude);
    }

    /// <summary>Dot product of two vectors.</summary>
    public static double Dot(Vector3D left, Vector3D right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    /// <summary>Angle between vectors in radians (range [0, π]).</summary>
    /// <exception cref="InvalidOperationException">Either vector has zero magnitude.</exception>
    public static double AngleBetween(Vector3D left, Vector3D right)
    {
        var denominator = left.Magnitude * right.Magnitude;
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot compute an angle with a zero vector.");
        var cosine = Clamp(Dot(left, right) / denominator, -1d, 1d);
        return Math.Acos(cosine);
    }

    /// <summary>Orthogonal projection of <paramref name="vector" /> onto the direction of <paramref name="onto" />.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="onto" /> is the zero vector.</exception>
    public static Vector3D Project(Vector3D vector, Vector3D onto)
    {
        var denominator = Dot(onto, onto);
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot project onto a zero vector.");
        var scalar = Dot(vector, onto) / denominator;
        return onto * scalar;
    }

    /// <summary>Right-handed cross product <c>left × right</c>.</summary>
    public static Vector3D Cross(Vector3D left, Vector3D right)
        => new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);

    public static Vector3D operator +(Vector3D left, Vector3D right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3D operator -(Vector3D left, Vector3D right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3D operator *(Vector3D vector, double scalar) => new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}