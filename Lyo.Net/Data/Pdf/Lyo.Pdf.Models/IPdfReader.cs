using Lyo.Metrics;

namespace Lyo.Pdf.Models;

/// <summary>Open PDF (PdfPig + byte snapshot). Dispose releases PdfPig; not thread-safe.</summary>
public interface IPdfReader : IDisposable, IAsyncDisposable
{
    ReadOnlyMemory<byte> SourceBytes { get; }

    IMetrics Metrics { get; }

    /// <summary>Text, layout, tables, and section navigation for this file.</summary>
    ITextExtractor Text { get; }

    PdfInfo GetInfo();

    /// <summary>Page width and height in PDF points for a 1-based page index.</summary>
    (double Width, double Height) GetPageSizePoints(int pageNumber1Based);
}