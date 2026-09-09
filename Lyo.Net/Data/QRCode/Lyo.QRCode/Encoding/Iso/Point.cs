using System.Reflection;

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>2D point with integer coordinates.</summary>
    private readonly struct Point : IEquatable<Point>
    {
        /// <summary>X-coordinate of the point.</summary>
        public int X { get; }

        /// <summary>Y-coordinate of the point.</summary>
        public int Y { get; }

        /// <summary>Builds a <see cref="Point" /> with the given X and Y coordinates.</summary>
        /// <param name="x">X-coordinate of the point.</param>
        /// <param name="y">Y-coordinate of the point.</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Whether <paramref name="other" /> has the same X and Y as this point.</summary>
        /// <param name="other"><see cref="Point" /> to compare with this instance.</param>
        /// <returns>True when both points share the same X and Y; otherwise false.</returns>
        /// <remarks>
        /// Without this <see cref="IEquatable{T}.Equals(T)" /> implementation, methods such as <see cref="List{T}.Contains(T)" /> fall back to reflection, which allocates
        /// during <see cref="FieldInfo.GetValue(object)" />.
        /// </remarks>
        public bool Equals(Point other) => X == other.X && Y == other.Y;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Point point && Equals(point);

        /// <inheritdoc />
        public override int GetHashCode()
#if NET5_0_OR_GREATER
            => HashCode.Combine(X, Y);
#else
            => X ^ (int)(((uint)Y << 16) | ((uint)Y >> 16));
#endif
    }
}