using Lyo.Common;

namespace Lyo.Pdf.Models;

/// <summary>Service for loading, parsing, and extracting content from PDF files.</summary>
public interface IPdfService
{
    Task<LoadedPdfLease> LoadPdfFromFileAsync(string filePath, CancellationToken ct = default);

    LoadedPdfLease LoadPdfFromFile(string filePath);

    Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    IReadOnlyList<LoadedPdfLease> LoadPdfsFromFiles(params string[] filePaths);

    Task<LoadedPdfLease> LoadPdfFromUrlAsync(string url, CancellationToken ct = default);

    LoadedPdfLease LoadPdfFromUrl(string url);

    Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default);

    IReadOnlyList<LoadedPdfLease> LoadPdfsFromUrls(params string[] urls);

    Task<LoadedPdfLease> LoadPdfFromBytesAsync(byte[] pdfBytes, CancellationToken ct = default);

    LoadedPdfLease LoadPdfFromBytes(byte[] pdfBytes);

    Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromBytesAsync(IEnumerable<byte[]> pdfs, CancellationToken ct = default);

    IReadOnlyList<LoadedPdfLease> LoadPdfsFromBytes(params byte[][] pdfs);

    Task<LoadedPdfLease> LoadPdfFromStreamAsync(Stream stream, CancellationToken ct = default);

    LoadedPdfLease LoadPdfFromStream(Stream stream);

    Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromStreamsAsync(IEnumerable<Stream> streams, CancellationToken ct = default);

    IReadOnlyList<LoadedPdfLease> LoadPdfsFromStreams(params Stream[] streams);

    Task UnloadPdfAsync(Guid pdfId, CancellationToken ct = default);

    void UnloadPdf(Guid pdfId);

    Task<PdfInfo> GetPdfInfoAsync(Guid pdfId, CancellationToken ct = default);

    PdfInfo GetPdfInfo(Guid pdfId);

    Task<IReadOnlyList<PdfWord>> GetWordsAsync(Guid pdfId, int? page = null, CancellationToken ct = default);

    IReadOnlyList<PdfWord> GetWords(Guid pdfId, int? page = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesAsync(Guid pdfId, int? page = null, double? yTolerance = null, CancellationToken ct = default);

    IReadOnlyList<PdfTextLine> GetLines(Guid pdfId, int? page = null, double? yTolerance = null);

    Task<IReadOnlyList<PdfWord>> GetWordsBetweenAsync(Guid pdfId, string? startText = null, string? endText = null, int? page = null, CancellationToken ct = default);

    IReadOnlyList<PdfWord> GetWordsBetween(Guid pdfId, string? startText = null, string? endText = null, int? page = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenAsync(
        Guid pdfId,
        string? startText = null,
        string? endText = null,
        int? page = null,
        double? yTolerance = null,
        CancellationToken ct = default);

    IReadOnlyList<PdfTextLine> GetLinesBetween(Guid pdfId, string? startText = null, string? endText = null, int? page = null, double? yTolerance = null);

    /// <summary>Gets text lines within a bounding box region on a specific page.</summary>
    Task<IReadOnlyList<PdfTextLine>> GetLinesInBoundingBoxAsync(Guid pdfId, PdfBoundingBox region, double? yTolerance = null, CancellationToken ct = default);

    /// <summary>Gets text lines within a bounding box region on a specific page.</summary>
    IReadOnlyList<PdfTextLine> GetLinesInBoundingBox(Guid pdfId, PdfBoundingBox region, double? yTolerance = null);

    /// <summary>
    /// Extracts text inside the region as separate columns. For two columns, a gutter is inferred from the largest horizontal gap on each line (when wide enough); for more than
    /// two columns, words are assigned using equal-width vertical bands across the region.
    /// </summary>
    Task<PdfColumnarText> GetColumnarTextInBoundingBoxAsync(Guid pdfId, PdfBoundingBox region, int columnCount, double? yTolerance = null, CancellationToken ct = default);

    /// <inheritdoc cref="GetColumnarTextInBoundingBoxAsync" />
    PdfColumnarText GetColumnarTextInBoundingBox(Guid pdfId, PdfBoundingBox region, int columnCount, double? yTolerance = null);

    /// <summary>Builds columnar plain text from words (e.g. from <see cref="GetWordsBetween" /> or a section).</summary>
    Task<PdfColumnarText> GetColumnarTextAsync(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null, CancellationToken ct = default);

    /// <inheritdoc cref="GetColumnarTextAsync" />
    PdfColumnarText GetColumnarText(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null);

    Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
        Guid pdfId,
        IEnumerable<string> knownKeys,
        int? page = null,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1,
        CancellationToken ct = default);

    IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
        Guid pdfId,
        IEnumerable<string> knownKeys,
        int? page = null,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1);

    Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
        IReadOnlyList<PdfWord> words,
        IEnumerable<string> knownKeys,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1,
        CancellationToken ct = default);

    IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
        IReadOnlyList<PdfWord> words,
        IEnumerable<string> knownKeys,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1);

    /// <summary>Detects key/value pairs when no keys are provided, using <paramref name="inferFlags" />.</summary>
    /// <param name="keyValueDelimiters">Punctuation characters to treat as label terminators (order matters); default is colon and semicolon.</param>
    IReadOnlyDictionary<string, string?> InferKeyValuePairsFromFormatting(
        IReadOnlyList<PdfWord> words,
        double yTolerance = 5.0,
        int columnCount = 1,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
        IReadOnlyList<char>? keyValueDelimiters = null);

    /// <summary>Detects a header row when headers are not provided, using <paramref name="inferFlags" />.</summary>
    /// <param name="keyValueDelimiters">When delimiter-based inference is enabled, punctuation characters to split on (order matters).</param>
    ColumnHeader[] InferTableHeadersFromFormatting(
        IReadOnlyList<PdfWord> words,
        double? yTolerance = null,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
        IReadOnlyList<char>? keyValueDelimiters = null);

    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
        Guid pdfId,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default);

    IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(Guid pdfId, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null,
        CancellationToken ct = default);

    /// <param name="inferFormattingForHeaderRows">When set (bold and/or underline), consecutive lines are merged into one header only while each line matches that inference styling; the next plain line starts data.</param>
    IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0, PdfInferFormattingFlags? inferFormattingForHeaderRows = null);

    /// <summary>Loads PDF from bytes, extracts table using headers, and returns DataTable result. Unloads the PDF after extraction.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] pdfBytes, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    /// <summary>Loads PDF from bytes, extracts table using headers, and returns DataTable result. Unloads the PDF after extraction.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] pdfBytes,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default);

    Task<byte[]> MergePdfsToFileAsync(IEnumerable<Guid> pdfIds, string filePath, CancellationToken ct = default);

    byte[] MergePdfsToFile(IEnumerable<Guid> pdfIds, string filePath);

    Task<byte[]> MergePdfsToStreamAsync(IEnumerable<Guid> pdfIds, Stream stream, CancellationToken ct = default);

    byte[] MergePdfsToStream(IEnumerable<Guid> pdfIds, Stream stream);

    Task<byte[]> MergePdfsAsync(IEnumerable<Guid> pdfIds, CancellationToken ct = default);

    byte[] MergePdfs(IEnumerable<Guid> pdfIds);

    Task<byte[]> MergePdfFilesAsync(string outputFilePath, string initialPdf, string[] paths, CancellationToken ct = default);

    byte[] MergePdfFiles(string outputFilePath, string initialPdf, params string[] paths);

    Task<byte[]> MergePdfFilesAsync(string outputFilePath, FileInfo initialPdf, FileInfo[] paths, CancellationToken ct = default);

    byte[] MergePdfFiles(string outputFilePath, FileInfo initialPdf, params FileInfo[] paths);

    Task<byte[]> MergePdfBytesAsync(string outputFilePath, byte[] initialPdf, byte[][] pdfs, CancellationToken ct = default);

    byte[] MergePdfBytes(string outputFilePath, byte[] initialPdf, params byte[][] pdfs);

    Task SavePdfAsync(Guid pdfId, string filePath, CancellationToken ct = default);

    void SavePdf(Guid pdfId, string filePath);

    Task SavePdfToStreamAsync(Guid pdfId, Stream stream, CancellationToken ct = default);

    void SavePdfToStream(Guid pdfId, Stream stream);

    Task<byte[]> GetPdfBytesAsync(Guid pdfId, CancellationToken ct = default);

    byte[] GetPdfBytes(Guid pdfId);

    /// <summary>Gets all words between the start section and the next known section, spanning multiple pages.</summary>
    /// <param name="pdfId">The loaded PDF identifier.</param>
    /// <param name="startSection">The section header to start from (e.g. "CHARGES").</param>
    /// <param name="sectionsInOrder">All section headers in document order (top to bottom). The end boundary is the first section after startSection that appears on each page.</param>
    /// <param name="defaultEndSection">When no end section is found on a page, use this as the end marker. If null, takes content to end of page.</param>
    /// <param name="startPage">Optional first page to process (1-based). Default 1.</param>
    /// <param name="endPage">Optional last page to process (1-based). Default last page.</param>
    /// <returns>All words from the section across the specified pages.</returns>
    IReadOnlyList<PdfWord> GetWordsBetweenSections(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null);

    /// <summary>Gets all lines (each line contains multiple words with bounding boxes) between the start section and the next known section, spanning multiple pages.</summary>
    /// <param name="pdfId">The loaded PDF identifier.</param>
    /// <param name="startSection">The section header to start from.</param>
    /// <param name="sectionsInOrder">All section headers in document order. The end boundary is the first section after startSection that appears on each page.</param>
    /// <param name="defaultEndSection">When no end section is found on a page, use this as the end marker. If null, takes content to end of page.</param>
    /// <param name="startPage">Optional first page to process (1-based). Default 1.</param>
    /// <param name="endPage">Optional last page to process (1-based). Default last page.</param>
    /// <param name="yTolerance">Y tolerance (points) for grouping words into lines. Default uses service configuration.</param>
    /// <returns>Lines from the section across the specified pages. Each line has Words with BoundingBox.</returns>
    IReadOnlyList<PdfTextLine> GetLinesBetweenSections(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null);

    /// <summary>Async version of GetLinesBetweenSections.</summary>
    Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenSectionsAsync(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default);

    /// <summary>Gets a section by name, spanning pages. Returns lines and a Words convenience property for extraction.</summary>
    /// <param name="pdfId">The loaded PDF identifier.</param>
    /// <param name="startSection">The section header to start from (e.g. "CHARGES").</param>
    /// <param name="sectionsInOrder">All section headers in document order. The end boundary is the first section after startSection that appears on each page.</param>
    /// <param name="defaultEndSection">When no end section is found on a page, use this as the end marker. If null, takes content to end of page.</param>
    /// <param name="startPage">Optional first page to process (1-based). Default 1.</param>
    /// <param name="endPage">Optional last page to process (1-based). Default last page.</param>
    /// <param name="yTolerance">Y tolerance (points) for grouping words into lines. Default uses service configuration.</param>
    /// <returns>The section with Lines and Words, or null if the section is not found.</returns>
    PdfSection? GetSection(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null);

    /// <summary>Async version of GetSection.</summary>
    Task<PdfSection?> GetSectionAsync(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default);

    /// <summary>Extracts a DataTable from a loaded PDF using column headers.</summary>
    DataTable.Models.DataTable ExtractDataTable(Guid pdfId, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    /// <summary>Extracts a DataTable from words (e.g. from GetSection(...).Words or GetWordsBetween).</summary>
    DataTable.Models.DataTable ExtractDataTable(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0);

    /// <summary>Async version of ExtractDataTable by pdfId.</summary>
    Task<DataTable.Models.DataTable> ExtractDataTableAsync(Guid pdfId, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0, CancellationToken ct = default);

    /// <summary>Async version of ExtractDataTable by words.</summary>
    Task<DataTable.Models.DataTable> ExtractDataTableAsync(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0, CancellationToken ct = default);
}