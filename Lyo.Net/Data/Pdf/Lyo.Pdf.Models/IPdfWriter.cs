namespace Lyo.Pdf.Models;

/// <summary>
/// PdfSharp-backed mutable PDF. Dispose to release the editor; not thread-safe.
/// Page indices use zero-based numbering.
/// </summary>
public interface IPdfWriter : IDisposable
{
    int PageCount { get; }

    void ImportPagesFrom(IPdfReader reader);

    void ImportPagesFrom(ReadOnlySpan<byte> pdfBytes);

    void RemovePage(int pageIndex);

    /// <summary>Inserts an empty portrait page before the existing page at <paramref name="pageIndex" />. Use PageCount to append.</summary>
    void InsertBlankPage(int pageIndex);

    /// <summary>Reorder pages: entry at index i is the current zero-based index of the page that should appear at position i after the operation.</summary>
    void ReorderPages(IReadOnlyList<int> newOrderFromOldIndices);

    byte[] ToBytes();

    void Save(string filePath);

    void CopyTo(Stream stream);

    Task SaveAsync(string filePath, CancellationToken ct = default);

    Task CopyToAsync(Stream stream, CancellationToken ct = default);
}
