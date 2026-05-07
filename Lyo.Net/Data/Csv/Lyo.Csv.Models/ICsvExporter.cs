using System.Reflection;

namespace Lyo.Csv.Models;

/// <summary>Exports data to CSV format. Supports enumerables, row/column dictionaries, <see cref="DataTable.Models.DataTable" />, property selection, and async export with progress.</summary>
public interface ICsvExporter
{
    /// <summary>Writes <paramref name="data" /> to <paramref name="csvFilePath" /> using CsvHelper and registered class maps.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath);

    /// <summary>Writes a nested row/column dictionary (0-based indices) to disk.</summary>
    /// <param name="data">Row index → column index → cell text.</param>
    /// <param name="csvFilePath">Destination file path.</param>
    /// <param name="hasHeaderRow">If true, the first data row is written as column headers.</param>
    void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true);

    /// <summary>Writes a nested row/column dictionary to <paramref name="csvStream" />.</summary>
    void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true);

    /// <summary>Serializes a nested row/column dictionary to a CSV string.</summary>
    string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    /// <summary>Serializes a nested row/column dictionary to CSV bytes.</summary>
    byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    /// <summary>Exports a Lyo data table to <paramref name="csvFilePath" />.</summary>
    void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath);

    /// <summary>Exports a Lyo data table to <paramref name="csvStream" />.</summary>
    void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream);

    /// <summary>Serializes a Lyo data table to a CSV string.</summary>
    string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Serializes a Lyo data table to CSV bytes.</summary>
    byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Writes <paramref name="data" /> to <paramref name="csvStream" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream);

    /// <summary>Writes <paramref name="data" /> to <paramref name="writer" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer);

    /// <summary>Serializes <paramref name="data" /> to a CSV string.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    string ExportToCsvString<T>(IEnumerable<T> data);

    /// <summary>Serializes <paramref name="data" /> to CSV bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToCsvBytes<T>(IEnumerable<T> data);

    /// <summary>Exports only <paramref name="selectedProperties" /> as columns to <paramref name="csvFilePath" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath);

    /// <summary>Exports only <paramref name="selectedProperties" /> as columns to <paramref name="csvStream" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream);

    /// <summary>Exports only <paramref name="selectedProperties" /> as columns to <paramref name="writer" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer);

    /// <summary>Serializes selected properties to a CSV string.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    /// <summary>Serializes selected properties to CSV bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

#if !NETSTANDARD2_0
    /// <summary>Asynchronously writes <paramref name="data" /> to <paramref name="csvFilePath" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously writes <paramref name="data" /> to <paramref name="csvStream" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default);

    /// <summary>Asynchronously writes <paramref name="data" /> to <paramref name="writer" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default);

    /// <summary>Asynchronously serializes <paramref name="data" /> to a CSV string.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    /// <summary>Asynchronously serializes <paramref name="data" /> to CSV bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    /// <summary>Asynchronously exports selected properties to <paramref name="csvFilePath" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously exports selected properties to <paramref name="csvStream" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports rows to a CSV stream with custom headers. Key = header text, value = property to read.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports rows to a CSV stream using formatter delegates. Key = header text, value = cell text per row.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct = default);

    /// <summary>Asynchronously exports selected properties to <paramref name="writer" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default);

    /// <summary>Asynchronously serializes selected properties to a CSV string.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    /// <summary>Asynchronously serializes selected properties to CSV bytes.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    /// <summary>Asynchronously writes a nested row/column dictionary to <paramref name="csvFilePath" />.</summary>
    Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Asynchronously writes a nested row/column dictionary to <paramref name="csvStream" />.</summary>
    Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Asynchronously serializes a nested dictionary to a CSV string.</summary>
    Task<string> ExportToCsvStringFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    /// <summary>Asynchronously serializes a nested dictionary to CSV bytes.</summary>
    Task<byte[]> ExportToCsvBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    /// <summary>Asynchronously exports a Lyo data table to <paramref name="csvFilePath" />.</summary>
    Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously exports a Lyo data table to <paramref name="csvStream" />.</summary>
    Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default);

    /// <summary>Asynchronously serializes a Lyo data table to a CSV string.</summary>
    Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Asynchronously serializes a Lyo data table to CSV bytes.</summary>
    Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Exports with periodic progress reports (every 100 rows or at completion).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    /// <summary>Exports to a stream with periodic progress reports.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    /// <summary>Appends rows to an existing file; optionally writes the header if the file is new or empty.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default);
#endif
}