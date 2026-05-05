using System.Reflection;

namespace Lyo.Xlsx.Models;

/// <summary>Exports data to XLSX (Excel) format via ClosedXML: typed rows, selected properties, multi-sheet workbooks, Lyo data tables, and async APIs on modern targets.</summary>
public interface IXlsxExporter
{
    /// <summary>Writes <paramref name="data"/> to <paramref name="xlsxFilePath"/>; optional <paramref name="worksheetName"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null);

    /// <summary>Writes <paramref name="data"/> to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null);

    /// <summary>Serializes <paramref name="data"/> to an XLSX byte array (single sheet).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null);

    /// <summary>Exports only <paramref name="selectedProperties"/> to <paramref name="xlsxFilePath"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null);

    /// <summary>Exports only <paramref name="selectedProperties"/> to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null);

    /// <summary>Serializes selected properties to XLSX bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null);

    /// <summary>Writes multiple worksheets (<paramref name="dataSets"/>) to <paramref name="xlsxFilePath"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath);

    /// <summary>Writes multiple worksheets to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream);

    /// <summary>Serializes multiple worksheets to bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets);

    /// <summary>Writes a row/column dictionary map to <paramref name="xlsxFilePath"/>; <paramref name="useHeaderRow"/> controls whether the first row is treated as headers.</summary>
    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true);

    /// <summary>Writes a row/column dictionary map to <paramref name="xlsxStream"/>.</summary>
    void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true);

    /// <summary>Serializes a row/column dictionary map to XLSX bytes.</summary>
    byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true);

    /// <summary>Exports a Lyo data table to <paramref name="xlsxFilePath"/>.</summary>
    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath);

    /// <summary>Exports a Lyo data table to <paramref name="xlsxStream"/>.</summary>
    void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream);

    /// <summary>Serializes a Lyo data table to XLSX bytes.</summary>
    byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable);

#if !NETSTANDARD2_0
    /// <summary>Asynchronously writes rows to <paramref name="xlsxFilePath"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously writes rows to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously serializes rows to XLSX bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously exports selected properties to <paramref name="xlsxFilePath"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Asynchronously exports selected properties to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Exports with custom headers: key = column title, value = property to read.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Exports using per-row formatters: key = column title, value = cell text factory.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default);

    /// <summary>Asynchronously serializes selected properties to XLSX bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null, CancellationToken ct = default);

    /// <summary>Asynchronously writes multiple worksheets to <paramref name="xlsxFilePath"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously writes multiple worksheets to <paramref name="xlsxStream"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Asynchronously serializes multiple worksheets to bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default);

    /// <summary>Asynchronously writes a nested dictionary to <paramref name="xlsxFilePath"/>.</summary>
    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Asynchronously writes a nested dictionary to <paramref name="xlsxStream"/>.</summary>
    Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Asynchronously serializes a nested dictionary to XLSX bytes.</summary>
    Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, CancellationToken ct = default);

    /// <summary>Asynchronously exports a Lyo data table to <paramref name="xlsxFilePath"/>.</summary>
    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously exports a Lyo data table to <paramref name="xlsxStream"/>.</summary>
    Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Asynchronously serializes a Lyo data table to XLSX bytes.</summary>
    Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);
#endif
}
