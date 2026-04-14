using Lyo.Pdf.Models;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Static convenience methods for PDF bounding box annotation without dependency injection.</summary>
public static class PdfAnnotator
{
    private static IPdfAnnotatorService? _default;

    public static IPdfAnnotatorService Default => _default ??= new BrowserPdfAnnotator();

    public static Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(Stream pdfStream, CancellationToken ct = default) => Default.AnnotateAsync(pdfStream, ct);

    public static Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(byte[] pdfBytes, CancellationToken ct = default) => Default.AnnotateAsync(pdfBytes, ct);

    public static Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFileAsync(string filePath, CancellationToken ct = default) => Default.AnnotateFileAsync(filePath, ct);

    public static void ResetDefault() => _default = null;
}