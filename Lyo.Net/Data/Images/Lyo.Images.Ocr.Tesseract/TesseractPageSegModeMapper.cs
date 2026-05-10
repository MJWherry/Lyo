using Tesseract;

namespace Lyo.Images.Ocr.Tesseract;

internal static class TesseractPageSegModeMapper
{
    public static PageSegMode ToPageSegMode(OcrPageSegmentationMode mode) => (PageSegMode)(int)mode;
}
