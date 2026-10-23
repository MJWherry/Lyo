using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot.Tests;

internal static class Frames
{
    internal static Image<Rgba32> Rows(int width, int height, int origin)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++) {
            var color = RowColor(origin + y);
            for (var x = 0; x < width; x++)
                image[x, y] = color;
        }

        return image;
    }

    internal static Image<Rgba32> Flat(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height, color);
        return image;
    }

    internal static Rgba32 RowColor(int row) => new((byte)(row % 200), (byte)(40 + row % 100), (byte)(row % 80), 255);

    internal static void AssertRows(Image<Rgba32> actual, int actualY, Image<Rgba32> expected, int expectedY, int height)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < actual.Width; x++)
            Assert.Equal(expected[x, expectedY + y], actual[x, actualY + y]);
    }
}