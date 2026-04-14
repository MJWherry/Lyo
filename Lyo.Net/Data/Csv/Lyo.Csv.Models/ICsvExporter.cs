using System.Reflection;

namespace Lyo.Csv.Models;

/// <summary>Exports data to CSV format. Supports exporting from enumerables, dictionaries, DataTables, with progress reporting and optional header control.</summary>
public interface ICsvExporter
{
    void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath);

    void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true);

    void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true);

    string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath);

    void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream);

    string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable);

    byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable);

    void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream);

    void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer);

    string ExportToCsvString<T>(IEnumerable<T> data);

    byte[] ExportToCsvBytes<T>(IEnumerable<T> data);

    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath);

    void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream);

    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer);

    string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

#if !NETSTANDARD2_0
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default);

    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to CSV stream with custom column headers. Key = header text, Value = property to read.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to CSV stream with formatter delegates. Key = header text, Value = function that returns the cell value for each row.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default);

    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    Task<string> ExportToCsvStringFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default);

    Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default);
#endif
}