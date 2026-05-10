namespace Lyo.Images.Ocr;

/// <summary>Error codes returned by OCR operations.</summary>
public static class OcrErrorCodes
{
    /// <summary>Input stream was null or empty.</summary>
    public const string ImageEmpty = "OCR_IMAGE_EMPTY";

    /// <summary>Failed to decode or load image bytes for OCR.</summary>
    public const string ImageLoadFailed = "OCR_IMAGE_LOAD_FAILED";

    /// <summary>OCR engine is not configured (e.g. missing tessdata path).</summary>
    public const string EngineNotConfigured = "OCR_ENGINE_NOT_CONFIGURED";

    /// <summary>Native Leptonica/Tesseract shared library could not be loaded (common on Linux when the NuGet expects different .so names than the distro ships).</summary>
    public const string NativeLibraryNotFound = "OCR_NATIVE_LIBRARY_NOT_FOUND";

    /// <summary>OCR processing failed.</summary>
    public const string RecognitionFailed = "OCR_RECOGNITION_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "OCR_OPERATION_CANCELLED";
}
