using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Vectors;

/// <summary>Two-dimensional Euclidean vector with finite components.</summary>
/// <remarks>Used by geometry and linear-system helpers; normalization rejects zero vectors.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Vector2D
{
    /// <summary>X component.</summary>
    public double X { get; }

    /// <summary>Y component.</summary>
    public double Y { get; }

    /// <summary>Euclidean length √(x² + y²).</summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y);

    /// <summary>Creates a vector after validating finite components.</summary>
    /// <param name="x">Finite X component.</param>
    /// <param name="y">Finite Y component.</param>
    public Vector2D(double x, double y)
    {
        X = MathValueGuards.Finite(x, nameof(x));
        Y = MathValueGuards.Finite(y, nameof(y));
    }

    /// <summary>Returns a unit vector in the same direction.</summary>
    /// <exception cref="InvalidOperationException">The vector has zero magnitude.</exception>
    public Vector2D Normalize()
    {
        OperationHelpers.ThrowIf(Magnitude == 0, "Cannot normalize a zero vector.");
        return new(X / Magnitude, Y / Magnitude);
    }

    /// <summary>Dot product of two vectors.</summary>
    public static double Dot(Vector2D left, Vector2D right) => left.X * right.X + left.Y * right.Y;

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    /// <summary>Angle between vectors in radians (range [0, π]).</summary>
    /// <exception cref="InvalidOperationException">Either vector has zero magnitude.</exception>
    public static double AngleBetween(Vector2D left, Vector2D right)
    {
        var denominator = left.Magnitude * right.Magnitude;
        OperationHelpers.ThrowIf(denominator == 0d, "Cannot compute an angle with a zero vector.");
        var cosine = Clamp(Dot(left, right) / denominator, -1d, 1d);
        return Math.Acos(cosine);
    }

    /// <summary>Orthogonal projection of <paramref name="vector" /> onto the direction of <paramref name="onto" />.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="onto" /> is the zero vector.</exception>
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