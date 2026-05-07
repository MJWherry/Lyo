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
}