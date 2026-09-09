using Lyo.Pdf.Models;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Annotates PDFs with bounding boxes in the browser. Returns ID-to-region map when the user finishes.</summary>
public interface IPdfAnnotatorService
{
    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(Stream pdfStream, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(byte[] pdfBytes, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFileAsync(string filePath, CancellationToken ct = default);
}