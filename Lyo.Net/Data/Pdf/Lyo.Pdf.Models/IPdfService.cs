namespace Lyo.Pdf.Models;

/// <summary>Loads PDFs, merges payloads, edits with PdfSharp, and exposes configurable defaults via <see cref="PdfServiceOptions" /> registration.</summary>
public interface IPdfService
{
    Task<IPdfReader> OpenFromFileAsync(string filePath, CancellationToken ct = default);

    IPdfReader OpenFromFile(string filePath);

    Task<IPdfReader> OpenFromUrlAsync(string url, CancellationToken ct = default);

    Task<IPdfReader> OpenFromBytesAsync(byte[] pdfBytes, CancellationToken ct = default);

    IPdfReader OpenFromBytes(byte[] pdfBytes);

    Task<IReadOnlyList<IPdfReader>> OpenFromFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    IReadOnlyList<IPdfReader> OpenFromFiles(params string[] filePaths);

    Task<IReadOnlyList<IPdfReader>> OpenFromUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default);

    Task<IReadOnlyList<IPdfReader>> OpenFromBytesBatchAsync(IEnumerable<byte[]> pdfs, CancellationToken ct = default);

    IReadOnlyList<IPdfReader> OpenFromBytesBatch(params byte[][] pdfs);

    Task<IPdfReader> OpenFromStreamAsync(Stream stream, CancellationToken ct = default);

    IPdfReader OpenFromStream(Stream stream);

    Task<IReadOnlyList<IPdfReader>> OpenFromStreamsAsync(IEnumerable<Stream> streams, CancellationToken ct = default);

    IReadOnlyList<IPdfReader> OpenFromStreams(params Stream[] streams);

    IPdfWriter CreateEmpty();

    IPdfWriter OpenForEdit(byte[] pdfBytes);

    IPdfWriter OpenForEdit(string filePath);

    IPdfWriter OpenForEdit(Stream stream);

    Task<IPdfWriter> OpenForEditAsync(byte[] pdfBytes, CancellationToken ct = default);

    Task<IPdfWriter> OpenForEditAsync(string filePath, CancellationToken ct = default);

    Task<IPdfWriter> OpenForEditAsync(Stream stream, CancellationToken ct = default);

    Task<byte[]> MergePdfsToFileAsync(IReadOnlyList<byte[]> pdfBuffers, string filePath, CancellationToken ct = default);

    byte[] MergePdfsToFile(IReadOnlyList<byte[]> pdfBuffers, string filePath);

    Task<byte[]> MergePdfsToStreamAsync(IReadOnlyList<byte[]> pdfBuffers, Stream stream, CancellationToken ct = default);

    byte[] MergePdfsToStream(IReadOnlyList<byte[]> pdfBuffers, Stream stream);

    Task<byte[]> MergePdfsAsync(IReadOnlyList<byte[]> pdfBuffers, CancellationToken ct = default);

    byte[] MergePdfs(IReadOnlyList<byte[]> pdfBuffers);

    Task<byte[]> MergePdfFilesAsync(string outputFilePath, string initialPdf, string[] paths, CancellationToken ct = default);

    byte[] MergePdfFiles(string outputFilePath, string initialPdf, params string[] paths);

    Task<byte[]> MergePdfFilesAsync(string outputFilePath, FileInfo initialPdf, FileInfo[] paths, CancellationToken ct = default);

    byte[] MergePdfFiles(string outputFilePath, FileInfo initialPdf, params FileInfo[] paths);

    Task<byte[]> MergePdfBytesAsync(string outputFilePath, byte[] initialPdf, byte[][] pdfs, CancellationToken ct = default);

    byte[] MergePdfBytes(string outputFilePath, byte[] initialPdf, params byte[][] pdfs);
}