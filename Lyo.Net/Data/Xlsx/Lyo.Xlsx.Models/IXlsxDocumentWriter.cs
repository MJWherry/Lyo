using System.Reflection;

namespace Lyo.Xlsx.Models;

/// <summary>
/// Incremental multi-sheet XLSX writing session. Each <c>AddSheet*</c> call streams one worksheet into the underlying workbook; disposing the session
/// finalizes the workbook. Sheet names must be unique within the session.
/// </summary>
public interface IXlsxDocumentWriter : IDisposable
{
    /// <summary>Streams a worksheet named <paramref name="sheetName" /> with one column per readable property of <typeparamref name="T" />.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void AddSheet<T>(string sheetName, IEnumerable<T> rows, CancellationToken ct = default);

    /// <summary>Streams a worksheet named <paramref name="sheetName" /> with only <paramref name="selectedProperties" /> as columns, in the given order.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void AddSheet<T>(string sheetName, IEnumerable<T> rows, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    /// <summary>Streams a Lyo data table as a worksheet named <paramref name="sheetName" />.</summary>
    void AddSheetFromDataTable(string sheetName, DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>
    /// Streams a row/column dictionary map as a worksheet named <paramref name="sheetName" />; <paramref name="useHeaderRow" /> controls whether the
    /// first row is treated as headers.
    /// </summary>
    void AddSheetFromDictionary(string sheetName, IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, CancellationToken ct = default);
}
