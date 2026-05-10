namespace Lyo.Images.Ocr.Tests;

/// <summary>Guards Tesseract <c>--psm</c> numeric alignment documented on <see cref="OcrPageSegmentationMode"/>.</summary>
public sealed class OcrPageSegmentationModeTests
{
    [Fact]
    public void Page_segmentation_modes_match_tesseract_cli_numbers()
    {
        Assert.Equal(0, (int)OcrPageSegmentationMode.OsdOnly);
        Assert.Equal(1, (int)OcrPageSegmentationMode.AutoOsd);
        Assert.Equal(3, (int)OcrPageSegmentationMode.Auto);
        Assert.Equal(6, (int)OcrPageSegmentationMode.SingleBlock);
        Assert.Equal(7, (int)OcrPageSegmentationMode.SingleLine);
        Assert.Equal(8, (int)OcrPageSegmentationMode.SingleWord);
        Assert.Equal(11, (int)OcrPageSegmentationMode.SparseText);
        Assert.Equal(13, (int)OcrPageSegmentationMode.RawLine);
    }
}
