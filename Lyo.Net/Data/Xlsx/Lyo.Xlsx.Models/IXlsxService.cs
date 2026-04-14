using System.Reflection;
using System.Text;
using Lyo.Common;
#if NET10_0_OR_GREATER
#endif

namespace Lyo.Xlsx.Models;

/// <summary>Service for reading, writing, and converting XLSX (Excel) files with support for export, import, and CSV conversion.</summary>
public interface IXlsxService
{
    /// <summary>Gets the XLSX exporter for writing data to XLSX format.</summary>
    IXlsxExporter Exporter { get; }

    /// <summary>Gets the XLSX importer for reading/parsing data from XLSX format.</summary>
    IXlsxImporter Importer { get; }

    // Synchronous Export Methods
    void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null);

    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null);

    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null);

    // Multi-sheet Excel export
    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath);

    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream);

    byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets);

    // Conversion Methods (XLSX to CSV)
    void ConvertXlsxToCsv(string xlsxPath, string outputCsvPath, Encoding? encoding = null);

    void ConvertXlsxToCsv(Stream inputStream, Stream outputStream, Encoding? encoding = null);

    void ConvertXlsxToCsv(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null);

    void ConvertXlsxToCsv(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null);

    byte[] ConvertXlsxToCsvBytes(byte[] xlsxBytes, Encoding? encoding = null);

    byte[] ConvertXlsxToCsvBytes(Stream inputStream, Encoding? encoding = null);

    // Parse to Dictionary (raw data access)
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream);

    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Exports XLSX bytes to an HTML document containing a table (first sheet).</summary>
    /// <param name="xlsxBytes">Raw XLSX file bytes.</param>
    /// <param name="useHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses XlsxService configuration.</param>
    /// <returns>Complete HTML document string with table.</returns>
    string ExportToHtmlTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes);

    IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> xlsxFilePaths, bool? useHeaderRow = null);

#if !NETSTANDARD2_0
    // Asynchronous Export Methods
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default);

    Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default);

    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default);

    Task ExportToXlsxAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports data to XLSX stream with custom column headers. Key = header text, Value = property to read.</summary>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Exports data to XLSX stream with formatter delegates. Key = header text, Value = function that returns the cell value for each row.</summary>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null, CancellationToken ct = default);

    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default);

    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default);

    Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default);

    // Asynchronous Conversion Methods (XLSX to CSV)
    Task ConvertXlsxToCsvAsync(string xlsxPath, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default);

    Task ConvertXlsxToCsvAsync(Stream inputStream, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default);

    Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    Task<byte[]> ConvertXlsxToCsvBytesAsync(byte[] xlsxBytes, Encoding? encoding = null, CancellationToken ct = default);

    Task<byte[]> ConvertXlsxToCsvBytesAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default);

    // Asynchronous Parse to Dictionary
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Exports XLSX bytes to an HTML document containing a table (first sheet).</summary>
    /// <param name="xlsxBytes">Raw XLSX file bytes.</param>
    /// <param name="useHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses XlsxService configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete HTML document string with table.</returns>
    Task<string> ExportToHtmlTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> xlsxFilePaths,
        bool? useHeaderRow = null,
        CancellationToken ct = default);
#endif
}