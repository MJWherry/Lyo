namespace Lyo.Pdf.Models;

/// <summary>PdfSharp-backed editable PDF. Dispose releases the editor; not safe for concurrent use. Page indices are zero-based.</summary>
public interface IPdfWriter : IDisposable
{
    int PageCount { get; }

    void ImportPagesFrom(IPdfReader reader);

    void ImportPagesFrom(ReadOnlySpan<byte> pdfBytes);

    void RemovePage(int pageIndex);

    /// <summary>Inserts a blank portrait page before the page at <paramref name="pageIndex" />. Pass PageCount to append.</summary>
    void InsertBlankPage(int pageIndex);

    /// <summary>Reorders pages: the value at index i is the current zero-based page that should occupy position i after the change.</summary>
    void ReorderPages(IReadOnlyList<int> newOrderFromOldIndices);

    byte[] ToBytes();

    void Save(string filePath);

    void CopyTo(Stream stream);

    Task SaveAsync(string filePath, CancellationToken ct = default);

    Task CopyToAsync(Stream stream, CancellationToken ct = default);
}