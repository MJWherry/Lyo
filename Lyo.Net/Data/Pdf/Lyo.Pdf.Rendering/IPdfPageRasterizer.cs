using Lyo.Result;

namespace Lyo.Pdf.Rendering;

/// <summary>Renders PDF pages to raster images (typically PNG).</summary>
public interface IPdfPageRasterizer
{
    /// <summary>Renders one page to PNG at the requested DPI.</summary>
    /// <param name="pdfBytes">Full PDF file bytes.</param>
    /// <param name="pageNumber1Based">1-based page index.</param>
    /// <param name="dpi">Raster resolution (both axes).</param>
    /// <param name="password">Optional owner/user password; empty string when none.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<PdfRasterPage>> RenderPageToPngAsync(
        ReadOnlyMemory<byte> pdfBytes,
        int pageNumber1Based,
        int dpi,
        string? password = null,
        CancellationToken cancellationToken = default);
}
