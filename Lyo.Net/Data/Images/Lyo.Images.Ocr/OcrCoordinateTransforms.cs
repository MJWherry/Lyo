using Lyo.Common.Records;

namespace Lyo.Images.Ocr;

/// <summary>Converts between top-left screen pixel coordinates and <see cref="BoundingBox2D"/> Y-up pixels.</summary>
public static class OcrCoordinateTransforms
{
    /// <summary>
    /// Maps a Tesseract-style rectangle (origin top-left, Y increasing downward) into <see cref="BoundingBox2D"/> with Y-up
    /// (origin bottom-left), matching <see cref="Lyo.Common.Records.BoundingBox2D"/> height semantics.
    /// </summary>
    public static BoundingBox2D FromTopLeftDownwardRect(int x, int y, int width, int height, int imageHeightPixels)
    {
        var left = (double)x;
        var right = (double)(x + width);
        var topYUp = imageHeightPixels - y;
        var bottomYUp = imageHeightPixels - (y + height);
        return new(left, right, topYUp, bottomYUp);
    }

    /// <summary>
    /// Maps a Y-up pixel box to PDF points using uniform scale (caller should rasterize with matching aspect ratio).
    /// </summary>
    /// <param name="boxPixelsYUp">Box in pixel coordinates (Y-up).</param>
    /// <param name="pageWidthPts">PDF page width in points.</param>
    /// <param name="pageHeightPts">PDF page height in points.</param>
    /// <param name="rasterWidthPx">Rendered image width.</param>
    /// <param name="rasterHeightPx">Rendered image height.</param>
    public static BoundingBox2D MapPixelBoxToPdfPoints(
        BoundingBox2D boxPixelsYUp,
        double pageWidthPts,
        double pageHeightPts,
        int rasterWidthPx,
        int rasterHeightPx)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rasterWidthPx, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rasterHeightPx, 0);

        var sx = pageWidthPts / rasterWidthPx;
        var sy = pageHeightPts / rasterHeightPx;
        return new(
            boxPixelsYUp.Left * sx,
            boxPixelsYUp.Right * sx,
            boxPixelsYUp.Top * sy,
            boxPixelsYUp.Bottom * sy);
    }
}
