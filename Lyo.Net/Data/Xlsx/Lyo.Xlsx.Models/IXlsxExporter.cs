using System.Reflection;
#if !NETSTANDARD2_0
#endif

namespace Lyo.Xlsx.Models;

/// <summary>Exports data to XLSX (Excel) format. Supports exporting from enumerables, dictionaries, DataTables, with single or multi-sheet workbooks.</summary>
public interface IXlsxExporter
{
    void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null);

    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null);

    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null);

    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null);

    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath);

    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream);

    byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets);

    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true);

    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true);

    byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true);

    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath);

    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream);

    byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable);

#if !NETSTANDARD2_0
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

    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, CancellationToken ct = default);

    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default);

    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default);

    Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);
#endif
}