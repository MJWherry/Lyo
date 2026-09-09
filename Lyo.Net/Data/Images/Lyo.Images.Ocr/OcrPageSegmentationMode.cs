namespace Lyo.Images.Ocr;

/// <summary>Tesseract-compatible page segmentation mode (numeric values match <c>tesseract</c> <c>--psm</c>).</summary>
public enum OcrPageSegmentationMode
{
    /// <summary>Orientation and script detection only.</summary>
    OsdOnly = 0,

    /// <summary>Automatic segmentation plus orientation and script detection.</summary>
    AutoOsd = 1,

    /// <summary>Automatic segmentation without orientation or script detection.</summary>
    AutoOnly = 2,

    /// <summary>Fully automatic segmentation, no orientation or script detection (starts as this).</summary>
    Auto = 3,

    /// <summary>Treat the page as one column of mixed-size text.</summary>
    SingleColumn = 4,

    /// <summary>Treat the page as one uniform, vertically aligned text block.</summary>
    SingleBlockVertText = 5,

    /// <summary>Treat the page as one uniform text block.</summary>
    SingleBlock = 6,

    /// <summary>Treat the image as one text line.</summary>
    SingleLine = 7,

    /// <summary>Treat the image as one word.</summary>
    SingleWord = 8,

    /// <summary>Treat the image as one word arranged in a circle.</summary>
    CircleWord = 9,

    /// <summary>Treat the image as one character.</summary>
    SingleChar = 10,

    /// <summary>Sparse text: recover as much text as possible, order not required.</summary>
    SparseText = 11,

    /// <summary>Sparse text plus orientation and script detection.</summary>
    SparseTextOsd = 12,

    /// <summary>Raw line: one text line, skipping Tesseract-specific line hacks.</summary>
    RawLine = 13
}