using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr.Tests;

public sealed class OcrModelsTests
{
    [Fact]
    public void OcrEngineOptions_Defaults()
    {
        var o = new OcrEngineOptions();
        Assert.False(o.EnableMetrics);
        Assert.Equal("eng", o.DefaultLanguages);
        Assert.Equal(OcrPageSegmentationMode.SparseTextOsd, o.DefaultPageSegmentationMode);
    }

    [Fact]
    public void OcrPageResult_Holds_Dimensions_And_Lines()
    {
        var w = new OcrWord("x", new(0, 1, 2, 0), 99);
        var line = new OcrLine("x", w.BoundingBoxPixels, [w]);
        var page = new OcrPageResult("x", [w], [line], 100, 50);
        Assert.Equal(100, page.ImageWidth);
        Assert.Equal(50, page.ImageHeight);
        Assert.Single(page.Lines);
        Assert.Equal("x", page.FullText);
    }

    [Fact]
    public void OcrReadRequest_All_Optional_Overrides_Null_By_Default()
    {
        var r = new OcrReadRequest();
        Assert.Null(r.Languages);
        Assert.Null(r.PageSegmentationMode);
        Assert.Null(r.MinimumConfidencePercent);
    }
}