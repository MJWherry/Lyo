using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Reads XLSX workbooks: sheet listing, first-sheet or sheet-scoped dictionary / Lyo data table parses, synchronous and asynchronous.</summary>
public interface IXlsxReader
{
    /// <summary>Parses the first worksheet from a file into row → column → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath);

    /// <summary>Parses the first worksheet from a stream into row → column → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream);

    /// <summary>Parses the first worksheet into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from a stream into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from bytes into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from bytes into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes);

    /// <summary>Lists worksheet names of a workbook file, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(string xlsxFilePath);

    /// <summary>Lists worksheet names of a workbook stream, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(Stream xlsxStream);

    /// <summary>Lists worksheet names of workbook bytes, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(byte[] xlsxBytes);

    /// <summary>Parses the named worksheet from a file into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet does not exist.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from a file into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet index is out of range.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, int sheetIndex);

    /// <summary>Parses the named worksheet from a stream into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet does not exist.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from a stream into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet index is out of range.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, int sheetIndex);

    /// <summary>Parses the named worksheet from bytes into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet does not exist.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from bytes into row → column → cell text.</summary>
    /// <exception cref="ArgumentException">Thrown when the sheet index is out of range.</exception>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, int sheetIndex);

    /// <summary>Parses the named worksheet from a file into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from a file into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses the named worksheet from a stream into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from a stream into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses the named worksheet from bytes into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from bytes into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of a workbook file into Lyo data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of a workbook stream into Lyo data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of workbook bytes into Lyo data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null);

#if !NETSTANDARD2_0
    /// <summary>Asynchronously parses the first worksheet from a file into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from a stream into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from a stream into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from bytes into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from bytes into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default);

    /// <summary>Asynchronously lists worksheet names of a workbook file, in workbook order.</summary>
    Task<IReadOnlyList<string>> ListSheetNamesAsync(string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously lists worksheet names of a workbook stream, in workbook order.</summary>
    Task<IReadOnlyList<string>> ListSheetNamesAsync(Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Asynchronously lists worksheet names of workbook bytes, in workbook order.</summary>
    Task<IReadOnlyList<string>> ListSheetNamesAsync(byte[] xlsxBytes, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from a file into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, string sheetName, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a file into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, int sheetIndex, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from a stream into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, string sheetName, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a stream into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, int sheetIndex, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from bytes into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, string sheetName, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from bytes into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, int sheetIndex, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from a file into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a file into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from a stream into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a stream into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from bytes into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from bytes into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of a workbook file into Lyo data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of a workbook stream into Lyo data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of workbook bytes into Lyo data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);
#endif
}