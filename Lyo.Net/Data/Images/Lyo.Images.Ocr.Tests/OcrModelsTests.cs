using Lyo.Common.Records;
using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr.Tests;

public sealed class OcrModelsTests
{
    [Fact]
    public void OcrEngineOptions_defaults()
    {
        var o = new OcrEngineOptions();
        Assert.False(o.EnableMetrics);
        Assert.Equal("eng", o.DefaultLanguages);
        Assert.Equal(OcrPageSegmentationMode.Auto, o.DefaultPageSegmentationMode);
    }

    [Fact]
    public void OcrPageResult_holds_dimensions_and_lines()
    {
        var w = new OcrWord("x", new BoundingBox2D(0, 1, 2, 0), 99);
        var line = new OcrLine("x", w.BoundingBoxPixels, [w]);
        var page = new OcrPageResult("x", [w], [line], 100, 50);
        Assert.Equal(100, page.ImageWidth);
        Assert.Equal(50, page.ImageHeight);
        Assert.Single(page.Lines);
        Assert.Equal("x", page.FullText);
    }

    [Fact]
    public void OcrReadRequest_all_optional_overrides_null_by_default()
    {
        var r = new OcrReadRequest();
        Assert.Null(r.Languages);
        Assert.Null(r.PageSegmentationMode);
        Assert.Null(r.MinimumConfidencePercent);
    }
}
