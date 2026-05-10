namespace Lyo.Images.Ocr;

/// <summary>Tesseract-compatible page segmentation mode (numeric values match <c>tesseract</c> <c>--psm</c>).</summary>
public enum OcrPageSegmentationMode
{
    /// <summary>Osd only.</summary>
    OsdOnly = 0,

    /// <summary>Automatic page segmentation with Osd.</summary>
    AutoOsd = 1,

    /// <summary>Automatic page segmentation, no Osd or orientation.</summary>
    AutoOnly = 2,

    /// <summary>Fully automatic page segmentation, no Osd (default).</summary>
    Auto = 3,

    /// <summary>Assume a single column of variable-sized text.</summary>
    SingleColumn = 4,

    /// <summary>Assume a single uniform block of vertically aligned text.</summary>
    SingleBlockVertText = 5,

    /// <summary>Assume a single uniform block of text.</summary>
    SingleBlock = 6,

    /// <summary>Treat the image as a single text line.</summary>
    SingleLine = 7,

    /// <summary>Treat the image as a single word.</summary>
    SingleWord = 8,

    /// <summary>Treat the image as a single word in a circle.</summary>
    CircleWord = 9,

    /// <summary>Treat the image as a single character.</summary>
    SingleChar = 10,

    /// <summary>Sparse text; find as much text as possible in no particular order.</summary>
    SparseText = 11,

    /// <summary>Sparse text with Osd.</summary>
    SparseTextOsd = 12,

    /// <summary>Raw line; treat the image as a single text line, bypassing Tesseract-specific hacks.</summary>
    RawLine = 13
}
