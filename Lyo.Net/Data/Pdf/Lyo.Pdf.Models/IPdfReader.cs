using Lyo.Metrics;

namespace Lyo.Pdf.Models;

/// <summary>Opened PDF (PdfPig plus a byte snapshot). Dispose frees PdfPig; not safe for concurrent use.</summary>
public interface IPdfReader : IDisposable, IAsyncDisposable
{
    ReadOnlyMemory<byte> SourceBytes { get; }

    IMetrics Metrics { get; }

    /// <summary>Text, layout, tables, and section navigation for this document.</summary>
    ITextExtractor Text { get; }

    PdfInfo GetInfo();

    /// <summary>Page width and height in PDF points for a 1-based page number.</summary>
    (double Width, double Height) GetPageSizePoints(int pageNumber1Based);
}