using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Service for reading, writing, and converting XLSX (Excel) files with support for export, import, and CSV conversion.</summary>
public interface IXlsxService
{
    /// <summary>Gets the XLSX writer for writing data to XLSX format.</summary>
    IXlsxWriter Writer { get; }

    /// <summary>Gets the XLSX reader for reading/parsing data from XLSX format.</summary>
    IXlsxReader Reader { get; }

    /// <summary>Exports rows to an XLSX file (optional worksheet name).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null);

    /// <summary>Exports rows to an XLSX stream.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null);

    /// <summary>Serializes rows to an XLSX byte array (single worksheet).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null);

    /// <summary>Exports only the given properties as columns to an XLSX file.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null);

    /// <summary>Exports only the given properties as columns to an XLSX stream.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null);

    /// <summary>Serializes selected properties to XLSX bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null);

    /// <summary>Exports multiple named worksheets from one workbook to a file.</summary>
    /// <typeparam name="T">Row type for each sheet.</typeparam>
    /// <param name="dataSets">Sheet name → rows.</param>
    /// <param name="xlsxFilePath">Destination path.</param>
    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath);

    /// <summary>Exports multiple named worksheets to a stream.</summary>
    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream);

    /// <summary>Exports multiple worksheets to a byte array.</summary>
    byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets);

    /// <summary>Exports a row/column dictionary map to an XLSX file.</summary>
    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true);

    /// <summary>Exports a row/column dictionary map to a stream.</summary>
    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true);

    /// <summary>Serializes a row/column dictionary map to XLSX bytes.</summary>
    byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true);

    /// <summary>Exports a Lyo data table to an XLSX file.</summary>
    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath);

    /// <summary>Exports a Lyo data table to a stream.</summary>
    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream);

    /// <summary>Serializes a Lyo data table to XLSX bytes.</summary>
    byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>
    /// Opens an incremental multi-sheet writing session on <paramref name="xlsxStream" />; dispose the session to finalize the workbook. The stream is
    /// left open for the caller.
    /// </summary>
    IXlsxDocumentWriter CreateDocumentWriter(Stream xlsxStream);

    /// <summary>Opens an incremental multi-sheet writing session on <paramref name="xlsxFilePath" />; dispose the session to finalize and close the file.</summary>
    IXlsxDocumentWriter CreateDocumentWriter(string xlsxFilePath);

    /// <summary>Converts an XLSX file on disk to a CSV file.</summary>
    /// <param name="xlsxPath">Source workbook path.</param>
    /// <param name="outputCsvPath">Destination CSV path.</param>
    /// <param name="encoding">Text encoding for the CSV output; null uses a default.</param>
    void ConvertXlsxToCsv(string xlsxPath, string outputCsvPath, Encoding? encoding = null);

    /// <summary>Converts an XLSX stream to a CSV stream.</summary>
    void ConvertXlsxToCsv(Stream inputStream, Stream outputStream, Encoding? encoding = null);

    /// <summary>Converts XLSX bytes to a CSV file.</summary>
    void ConvertXlsxToCsv(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null);

    /// <summary>Converts XLSX bytes to a CSV stream.</summary>
    void ConvertXlsxToCsv(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null);

    /// <summary>Converts XLSX bytes to CSV bytes.</summary>
    byte[] ConvertXlsxToCsvBytes(byte[] xlsxBytes, Encoding? encoding = null);

    /// <summary>Converts an XLSX stream to CSV bytes.</summary>
    byte[] ConvertXlsxToCsvBytes(Stream inputStream, Encoding? encoding = null);

    /// <summary>Parses the first worksheet into a nested dictionary (row → column → cell text).</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath);

    /// <summary>Parses a workbook stream into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream);

    /// <summary>Parses the first worksheet into a data table.</summary>
    /// <param name="xlsxFilePath">Path to the workbook.</param>
    /// <param name="useHeaderRow">When true, first row is headers. When false, synthetic columns. When null, uses configuration.</param>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null);

    /// <summary>Parses a workbook stream into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null);

    /// <summary>Parses XLSX bytes into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Exports XLSX bytes to an HTML document containing a table (first sheet).</summary>
    /// <param name="xlsxBytes">Raw XLSX file bytes.</param>
    /// <param name="useHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses XlsxService configuration.</param>
    /// <returns>Complete HTML document string with table.</returns>
    string ExportToHtmlTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from bytes into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes);

    /// <summary>Parses multiple XLSX files to data tables (one result per path, same order as input).</summary>
    IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> xlsxFilePaths, bool? useHeaderRow = null);

    /// <summary>Lists worksheet names of a workbook file, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(string xlsxFilePath);

    /// <summary>Lists worksheet names of a workbook stream, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(Stream xlsxStream);

    /// <summary>Lists worksheet names of workbook bytes, in workbook order.</summary>
    IReadOnlyList<string> ListSheetNames(byte[] xlsxBytes);

    /// <summary>Parses the named worksheet from a file into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from a file into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, int sheetIndex);

    /// <summary>Parses the named worksheet from a stream into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from a stream into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, int sheetIndex);

    /// <summary>Parses the named worksheet from bytes into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, string sheetName);

    /// <summary>Parses the worksheet at the given zero-based index from bytes into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, int sheetIndex);

    /// <summary>Parses the named worksheet from a file into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from a file into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses the named worksheet from a stream into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from a stream into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses the named worksheet from bytes into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null);

    /// <summary>Parses the worksheet at the given zero-based index from bytes into a data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of a workbook file into data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of a workbook stream into data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null);

    /// <summary>Parses every worksheet of workbook bytes into data tables, keyed by sheet name in workbook order.</summary>
    IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Splits a workbook into one XLSX file per worksheet.</summary>
    /// <returns>Paths of the created workbooks, in sheet order.</returns>
    IReadOnlyList<string> SplitXlsxBySheet(string xlsxFilePath, string outputDirectory);

    /// <summary>Splits a workbook stream into one XLSX stream per worksheet.</summary>
    /// <param name="input">Source workbook stream.</param>
    /// <param name="outputStreamFactory">Receives the sheet name and returns the destination stream for that sheet.</param>
    /// <param name="leaveOpen">When true, leaves <paramref name="input" /> open after the operation completes.</param>
    void SplitXlsxBySheet(Stream input, Func<string, Stream> outputStreamFactory, bool leaveOpen = false);

    /// <summary>Splits workbook bytes into one XLSX payload per worksheet, keyed by sheet name.</summary>
    IReadOnlyDictionary<string, byte[]> SplitXlsxBytesBySheet(byte[] xlsxBytes);

    /// <summary>Splits one worksheet into multiple workbooks with at most <paramref name="rowsPerFile" /> data rows each.</summary>
    /// <returns>Paths of the created part workbooks, in order.</returns>
    IReadOnlyList<string> SplitXlsxByRows(string xlsxFilePath, int rowsPerFile, string outputDirectory, string? sheetName = null);

    /// <summary>Splits one worksheet from a workbook stream into multiple output streams.</summary>
    /// <param name="input">Source workbook stream.</param>
    /// <param name="rowsPerFile">Maximum data rows per output part.</param>
    /// <param name="outputStreamFactory">Receives a 1-based part number and returns the destination stream for that part.</param>
    /// <param name="sheetName">Optional worksheet name; when null, uses the first worksheet.</param>
    /// <param name="leaveOpen">When true, leaves <paramref name="input" /> open after the operation completes.</param>
    void SplitXlsxByRows(Stream input, int rowsPerFile, Func<int, Stream> outputStreamFactory, string? sheetName = null, bool leaveOpen = false);

    /// <summary>Splits one worksheet from workbook bytes into multiple XLSX payloads.</summary>
    IReadOnlyList<byte[]> SplitXlsxBytesByRows(byte[] xlsxBytes, int rowsPerFile, string? sheetName = null);

    /// <summary>Merges multiple XLSX files into one workbook.</summary>
    void MergeXlsxFiles(IEnumerable<string> inputFiles, string outputFile, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets);

    /// <summary>Merges multiple XLSX streams into one output stream.</summary>
    void MergeXlsxStreams(IEnumerable<Stream> inputs, Stream output, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets, bool leaveOpen = false);

    /// <summary>Merges multiple XLSX byte arrays into one workbook payload.</summary>
    byte[] MergeXlsxBytes(IEnumerable<byte[]> inputs, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets);

#if !NETSTANDARD2_0
    /// <summary>Exports rows to an XLSX file asynchronously.</summary>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports rows to an XLSX stream asynchronously.</summary>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Serializes rows to XLSX bytes asynchronously.</summary>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports selected properties to an XLSX file asynchronously.</summary>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Exports selected properties to an XLSX stream asynchronously.</summary>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports data to an XLSX stream with custom column headers. Key = header text, Value = property to read.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Exports data to an XLSX stream with formatter delegates. Key = header text, Value = cell value factory.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Serializes selected properties to XLSX bytes asynchronously.</summary>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports multiple worksheets to a file asynchronously.</summary>
    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Exports multiple worksheets to a stream asynchronously.</summary>
    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Exports multiple worksheets to bytes asynchronously.</summary>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default);

    /// <summary>Converts an XLSX file to CSV asynchronously.</summary>
    Task ConvertXlsxToCsvAsync(string xlsxPath, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Converts XLSX stream to CSV stream asynchronously.</summary>
    Task ConvertXlsxToCsvAsync(Stream inputStream, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Converts XLSX bytes to a CSV file asynchronously.</summary>
    Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Converts XLSX bytes to a CSV stream asynchronously.</summary>
    Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Converts XLSX bytes to CSV bytes asynchronously.</summary>
    Task<byte[]> ConvertXlsxToCsvBytesAsync(byte[] xlsxBytes, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Converts an XLSX stream to CSV bytes asynchronously.</summary>
    Task<byte[]> ConvertXlsxToCsvBytesAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Parses a workbook file into a nested dictionary asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Parses a workbook stream into a nested dictionary asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Parses a workbook file into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses a workbook stream into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses XLSX bytes into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Exports XLSX bytes to an HTML document containing a table (first sheet).</summary>
    /// <param name="xlsxBytes">Raw XLSX file bytes.</param>
    /// <param name="useHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses XlsxService configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete HTML document string with table.</returns>
    Task<string> ExportToHtmlTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses multiple XLSX files to data tables asynchronously.</summary>
    Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> xlsxFilePaths,
        bool? useHeaderRow = null,
        CancellationToken ct = default);

    /// <summary>Exports a row/column dictionary map to an XLSX file asynchronously.</summary>
    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Exports a row/column dictionary map to a stream asynchronously.</summary>
    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Serializes a row/column dictionary map to XLSX bytes asynchronously.</summary>
    Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, CancellationToken ct = default);

    /// <summary>Exports a Lyo data table to an XLSX file asynchronously.</summary>
    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Exports a Lyo data table to a stream asynchronously.</summary>
    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Serializes a Lyo data table to XLSX bytes asynchronously.</summary>
    Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Parses the first worksheet from bytes into a nested dictionary asynchronously.</summary>
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

    /// <summary>Asynchronously parses the named worksheet from a file into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a file into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from a stream into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from a stream into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the named worksheet from bytes into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the worksheet at the given zero-based index from bytes into a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of a workbook file into data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of a workbook stream into data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses every worksheet of workbook bytes into data tables, keyed by sheet name in workbook order.</summary>
    Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously splits a workbook into one XLSX file per worksheet.</summary>
    Task<IReadOnlyList<string>> SplitXlsxBySheetAsync(string xlsxFilePath, string outputDirectory, CancellationToken ct = default);

    /// <summary>Asynchronously splits a workbook stream into one XLSX stream per worksheet.</summary>
    Task SplitXlsxBySheetAsync(Stream input, Func<string, Stream> outputStreamFactory, bool leaveOpen = false, CancellationToken ct = default);

    /// <summary>Asynchronously splits workbook bytes into one XLSX payload per worksheet, keyed by sheet name.</summary>
    Task<IReadOnlyDictionary<string, byte[]>> SplitXlsxBytesBySheetAsync(byte[] xlsxBytes, CancellationToken ct = default);

    /// <summary>Asynchronously splits one worksheet into multiple workbooks with at most <paramref name="rowsPerFile" /> data rows each.</summary>
    Task<IReadOnlyList<string>> SplitXlsxByRowsAsync(string xlsxFilePath, int rowsPerFile, string outputDirectory, string? sheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously splits one worksheet from a workbook stream into multiple output streams.</summary>
    Task SplitXlsxByRowsAsync(Stream input, int rowsPerFile, Func<int, Stream> outputStreamFactory, string? sheetName = null, bool leaveOpen = false, CancellationToken ct = default);

    /// <summary>Asynchronously splits one worksheet from workbook bytes into multiple XLSX payloads.</summary>
    Task<IReadOnlyList<byte[]>> SplitXlsxBytesByRowsAsync(byte[] xlsxBytes, int rowsPerFile, string? sheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously merges multiple XLSX files into one workbook.</summary>
    Task MergeXlsxFilesAsync(IEnumerable<string> inputFiles, string outputFile, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets, CancellationToken ct = default);

    /// <summary>Asynchronously merges multiple XLSX streams into one output stream.</summary>
    Task MergeXlsxStreamsAsync(IEnumerable<Stream> inputs, Stream output, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets, bool leaveOpen = false, CancellationToken ct = default);

    /// <summary>Asynchronously merges multiple XLSX byte arrays into one workbook payload.</summary>
    Task<byte[]> MergeXlsxBytesAsync(IEnumerable<byte[]> inputs, XlsxMergeMode mode = XlsxMergeMode.PreserveSheets, CancellationToken ct = default);
#endif
}