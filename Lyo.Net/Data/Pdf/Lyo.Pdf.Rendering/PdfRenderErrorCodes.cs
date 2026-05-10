namespace Lyo.Pdf.Rendering;

/// <summary>Error codes for PDF rasterization.</summary>
public static class PdfRenderErrorCodes
{
    /// <summary>Rendering failed (invalid PDF, missing password, PDFium error, etc.).</summary>
    public const string RenderFailed = "PDF_RENDER_FAILED";

    /// <summary>Page index out of range.</summary>
    public const string PageOutOfRange = "PDF_RENDER_PAGE_OUT_OF_RANGE";
}
