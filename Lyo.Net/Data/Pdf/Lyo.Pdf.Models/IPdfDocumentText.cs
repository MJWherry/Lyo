using Lyo.Result;

namespace Lyo.Pdf.Models;

/// <summary>
/// Text/layout/table extraction for one <see cref="IPdfReader" /> (access via <see cref="IPdfReader.Text" />). Word-only overloads do not scan the PdfPig document; defaults
/// follow <see cref="PdfServiceOptions" /> established at load time.
/// </summary>
public interface IPdfDocumentText
{
    Task<IReadOnlyList<PdfWord>> GetWordsAsync(int? page = null, CancellationToken ct = default);

    IReadOnlyList<PdfWord> GetWords(int? page = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesAsync(int? page = null, double? yTolerance = null, CancellationToken ct = default);

    IReadOnlyList<PdfTextLine> GetLines(int? page = null, double? yTolerance = null);

    Task<IReadOnlyList<PdfWord>> GetWordsBetweenAsync(string? startText = null, string? endText = null, int? page = null, CancellationToken ct = default);

    IReadOnlyList<PdfWord> GetWordsBetween(string? startText = null, string? endText = null, int? page = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenAsync(
        string? startText = null,
        string? endText = null,
        int? page = null,
        double? yTolerance = null,
        CancellationToken ct = default);

    IReadOnlyList<PdfTextLine> GetLinesBetween(string? startText = null, string? endText = null, int? page = null, double? yTolerance = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesInBoundingBoxAsync(PdfBoundingBox region, double? yTolerance = null, CancellationToken ct = default);

    IReadOnlyList<PdfTextLine> GetLinesInBoundingBox(PdfBoundingBox region, double? yTolerance = null);

    Task<PdfColumnarText> GetColumnarTextInBoundingBoxAsync(PdfBoundingBox region, int columnCount, double? yTolerance = null, CancellationToken ct = default);

    PdfColumnarText GetColumnarTextInBoundingBox(PdfBoundingBox region, int columnCount, double? yTolerance = null);

    Task<PdfColumnarText> GetColumnarTextAsync(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null, CancellationToken ct = default);

    PdfColumnarText GetColumnarText(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null);

    Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
        IEnumerable<string> knownKeys,
        int? page = null,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1,
        CancellationToken ct = default);

    IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
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

    IReadOnlyDictionary<string, string?> InferKeyValuePairsFromFormatting(
        IReadOnlyList<PdfWord> words,
        double yTolerance = 5.0,
        int columnCount = 1,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
        IReadOnlyList<char>? keyValueDelimiters = null);

    ColumnHeader[] InferTableHeadersFromFormatting(
        IReadOnlyList<PdfWord> words,
        double? yTolerance = null,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
        IReadOnlyList<char>? keyValueDelimiters = null);

    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0, CancellationToken ct = default);

    IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null,
        CancellationToken ct = default);

    IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null);

    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] pdfBytes, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] pdfBytes,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default);

    DataTable.Models.DataTable ExtractDataTable(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0);

    DataTable.Models.DataTable ExtractDataTable(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0);

    Task<DataTable.Models.DataTable> ExtractDataTableAsync(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0, CancellationToken ct = default);

    Task<DataTable.Models.DataTable> ExtractDataTableAsync(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0, CancellationToken ct = default);
}