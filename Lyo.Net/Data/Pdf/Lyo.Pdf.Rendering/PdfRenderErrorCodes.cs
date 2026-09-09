namespace Lyo.Pdf.Rendering;

/// <summary>Error codes raised by PDF rasterization.</summary>
public static class PdfRenderErrorCodes
{
    /// <summary>Rasterization failed (bad PDF, missing password, PDFium error, etc.).</summary>
    public const string RenderFailed = "PDF_RENDER_FAILED";

    /// <summary>Requested page index is outside the document.</summary>
    public const string PageOutOfRange = "PDF_RENDER_PAGE_OUT_OF_RANGE";
}