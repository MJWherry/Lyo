using Lyo.Exceptions;
using Lyo.Pdf.Models;
using PdfSharp.Pdf.IO;
using SharpPdfDoc = PdfSharp.Pdf.PdfDocument;
using SharpPdfReaderIO = PdfSharp.Pdf.IO.PdfReader;

namespace Lyo.Pdf.Editing;

/// <summary>PdfSharp-backed mutable PDF (writer); zero-based page indices; not thread-safe.</summary>
public sealed class PdfWriter : IPdfWriter
{
    private SharpPdfDoc _pdf;
    private int _disposed;

    internal PdfWriter(SharpPdfDoc pdfDocument) => _pdf = pdfDocument ?? throw new ArgumentNullException(nameof(pdfDocument));

    /// <summary>Underlying PdfSharp document.</summary>
    public SharpPdfDoc PdfSharp => EnsureOpen();

    public int PageCount => EnsureOpen().PageCount;

    public void ImportPagesFrom(IPdfReader reader)
    {
        ArgumentHelpers.ThrowIfNull(reader);
        ImportPagesFrom(reader.SourceBytes.Span);
    }

    public void ImportPagesFrom(ReadOnlySpan<byte> pdfBytes)
    {
        var doc = EnsureOpen();
        OperationHelpers.ThrowIf(pdfBytes.IsEmpty, $"{nameof(pdfBytes)} must not be empty.");
        using var inputStream = new MemoryStream(pdfBytes.ToArray(), false);
        using var sourceDocument = SharpPdfReaderIO.Open(inputStream, PdfDocumentOpenMode.Import);
        for (var pageIndex = 0; pageIndex < sourceDocument.PageCount; pageIndex++)
            doc.AddPage(sourceDocument.Pages[pageIndex]);
    }

    public void RemovePage(int pageIndex)
    {
        var doc = EnsureOpen();
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var indices = Enumerable.Range(0, doc.PageCount).Where(i => i != pageIndex).ToArray();
        ReplacePages(doc, indices);
    }

    public void InsertBlankPage(int pageIndex)
    {
        var doc = EnsureOpen();
        if (pageIndex < 0 || pageIndex > doc.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        using var blob = new MemoryStream();
        doc.Save(blob);
        blob.Position = 0;
        using var import = SharpPdfReaderIO.Open(blob, PdfDocumentOpenMode.Import);
        var output = new SharpPdfDoc();
        for (var i = 0; i < import.PageCount; i++) {
            if (i == pageIndex)
                output.AddPage();

            output.AddPage(import.Pages[i]);
        }

        if (pageIndex == import.PageCount)
            output.AddPage();

        SwapDocument(output);
    }

    public void ReorderPages(IReadOnlyList<int> newOrderFromOldIndices)
    {
        ArgumentHelpers.ThrowIfNull(newOrderFromOldIndices);
        var doc = EnsureOpen();
        var n = doc.PageCount;
        OperationHelpers.ThrowIf(newOrderFromOldIndices.Count != n, $"{nameof(newOrderFromOldIndices)} length must equal current page count ({n}).");
        ThrowUnlessValidPermutationIndices(newOrderFromOldIndices, n);
        if (Enumerable.Range(0, n).Zip(newOrderFromOldIndices, (a, b) => a == b).All(eq => eq))
            return;

        ReplacePages(doc, newOrderFromOldIndices);
    }

    public byte[] ToBytes()
    {
        var doc = EnsureOpen();
        using var outputStream = new MemoryStream();
        doc.Save(outputStream);
        return outputStream.ToArray();
    }

    public void Save(string filePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var doc = EnsureOpen();
#if NETSTANDARD2_0
        using var ms = new MemoryStream();
        doc.Save(ms);
        File.WriteAllBytes(filePath, ms.ToArray());
#else
        doc.Save(filePath);
#endif
    }

    public void CopyTo(Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var doc = EnsureOpen();
#if NETSTANDARD2_0
        doc.Save(stream);
#else
        if (stream.CanSeek && stream.CanWrite)
            stream.SetLength(0);
        doc.Save(stream);
#endif
    }

#if NETSTANDARD2_0
    public Task SaveAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        ct.ThrowIfCancellationRequested();
        Save(filePath);
        return Task.CompletedTask;
    }

    public Task CopyToAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        ct.ThrowIfCancellationRequested();
        CopyTo(stream);
        return Task.CompletedTask;
    }
#else
    public Task SaveAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() => Save(filePath), ct);

    public Task CopyToAsync(Stream stream, CancellationToken ct = default)
        => Task.Run(() => CopyTo(stream), ct);
#endif

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try {
            _pdf.Dispose();
        }
        finally {
            _pdf = null!;
        }
    }

    private SharpPdfDoc EnsureOpen()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(PdfWriter));

        return _pdf;
    }

    private void SwapDocument(SharpPdfDoc next)
    {
        _pdf.Dispose();
        _pdf = next;
    }

    /// <summary>Reorder or filter pages: <paramref name="pageIndexesToTakeInOrder" /> index into old document.</summary>
    private void ReplacePages(SharpPdfDoc current, IReadOnlyList<int> pageIndexesToTakeInOrder)
    {
        using var blob = new MemoryStream();
        current.Save(blob);
        blob.Position = 0;
        using var import = SharpPdfReaderIO.Open(blob, PdfDocumentOpenMode.Import);
        var output = new SharpPdfDoc();
        foreach (var oi in pageIndexesToTakeInOrder)
            output.AddPage(import.Pages[oi]);

        SwapDocument(output);
    }

    private static void ThrowUnlessValidPermutationIndices(IReadOnlyList<int> order, int pageCount)
    {
        OperationHelpers.ThrowIf(order.Count != pageCount, $"{nameof(order)} length must equal page count ({pageCount}).");
        foreach (var i in order)
            OperationHelpers.ThrowIf(i < 0 || i >= pageCount, $"{nameof(order)} indices must fall in [0,{pageCount}).");

        var set = new HashSet<int>();
        foreach (var i in order)
            OperationHelpers.ThrowIf(!set.Add(i), $"{nameof(order)} must list each index exactly once.");
    }
}