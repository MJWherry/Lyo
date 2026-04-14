using Lyo.Pdf.Models;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Service for annotating PDFs with bounding boxes in the browser. Returns a dictionary of IDs to regions when the user completes.</summary>
public interface IPdfAnnotatorService
{
    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(Stream pdfStream, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(byte[] pdfBytes, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFileAsync(string filePath, CancellationToken ct = default);
}