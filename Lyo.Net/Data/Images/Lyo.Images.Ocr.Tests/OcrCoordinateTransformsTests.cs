using Lyo.Common.Records;

namespace Lyo.Images.Ocr.Tests;

public sealed class OcrCoordinateTransformsTests
{
    [Fact]
    public void FromTopLeftDownwardRect_maps_to_y_up()
    {
        var imageHeight = 100;
        // Top-left rect y=10, h=20 → spans pixel y 10..30; Y-up: bottom=70, top=90
        var box = OcrCoordinateTransforms.FromTopLeftDownwardRect(5, 10, 40, 20, imageHeight);
        Assert.Equal(5, box.Left);
        Assert.Equal(45, box.Right);
        Assert.Equal(90, box.Top);
        Assert.Equal(70, box.Bottom);
        Assert.Equal(20, box.Height);
    }

    [Fact]
    public void MapPixelBoxToPdfPoints_scales_uniformly()
    {
        var pixelBox = new BoundingBox2D(0, 100, 200, 0);
        var pdf = OcrCoordinateTransforms.MapPixelBoxToPdfPoints(pixelBox, pageWidthPts: 612, pageHeightPts: 792, rasterWidthPx: 100, rasterHeightPx: 200);
        Assert.Equal(0, pdf.Left);
        Assert.Equal(612, pdf.Right);
        Assert.Equal(792, pdf.Top);
        Assert.Equal(0, pdf.Bottom);
    }

    [Fact]
    public void FromTopLeftDownwardRect_top_row_maps_to_top_y_up_band()
    {
        var h = 50;
        // y=0 full width row, height 10 → bottom edge at y=10 in top-left coords → Y-up bottom = 50 - 10 = 40, top = 50
        var box = OcrCoordinateTransforms.FromTopLeftDownwardRect(0, 0, 200, 10, h);
        Assert.Equal(0, box.Left);
        Assert.Equal(200, box.Right);
        Assert.Equal(50, box.Top);
        Assert.Equal(40, box.Bottom);
    }

    [Fact]
    public void MapPixelBoxToPdfPoints_non_square_raster_scales_xy_independently()
    {
        var pixelBox = new BoundingBox2D(10, 20, 100, 50);
        var pdf = OcrCoordinateTransforms.MapPixelBoxToPdfPoints(pixelBox, pageWidthPts: 300, pageHeightPts: 400, rasterWidthPx: 100, rasterHeightPx: 200);
        Assert.Equal(30, pdf.Left);
        Assert.Equal(60, pdf.Right);
        Assert.Equal(200, pdf.Top);
        Assert.Equal(100, pdf.Bottom);
    }

    [Fact]
    public void MapPixelBoxToPdfPoints_zero_width_raster_throws()
    {
        var pixelBox = new BoundingBox2D(0, 1, 1, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OcrCoordinateTransforms.MapPixelBoxToPdfPoints(pixelBox, 100, 100, rasterWidthPx: 0, rasterHeightPx: 10));
    }
}
