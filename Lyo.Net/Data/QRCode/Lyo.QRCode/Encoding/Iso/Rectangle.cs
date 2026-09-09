namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Rectangle defined by top-left coordinates, width, and height.</summary>
    private readonly struct Rectangle
    {
        /// <summary>X-coordinate of the top-left corner.</summary>
        public int X { get; }

        /// <summary>Y-coordinate of the top-left corner.</summary>
        public int Y { get; }

        /// <summary>Width of the rectangle.</summary>
        public int Width { get; }

        /// <summary>Height of the rectangle.</summary>
        public int Height { get; }

        /// <summary>Builds a <see cref="Rectangle" /> from top-left coordinates, width, and height.</summary>
        /// <param name="x">X-coordinate of the top-left corner.</param>
        /// <param name="y">Y-coordinate of the top-left corner.</param>
        /// <param name="w">Width of the rectangle.</param>
        /// <param name="h">Height of the rectangle.</param>
        public Rectangle(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }
    }
}