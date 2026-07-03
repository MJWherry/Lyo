using System.Reflection;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;
#if NETSTANDARD2_0
using Lyo.Common;
#endif

namespace Lyo.Xlsx;

/// <summary>
/// Streams worksheets one at a time into a single XLSX package via <see cref="OpenXmlStreamWriter" />; disposing finalizes the workbook (and closes the
/// destination stream only when this session owns it, i.e. it was created from a file path).
/// </summary>
internal sealed class XlsxDocumentWriter : IXlsxDocumentWriter
{
    private readonly ILogger _logger;
    private readonly Stream? _ownedStream;

    private readonly HashSet<string> _sheetNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly OpenXmlStreamWriter _writer;
    private bool _disposed;

    internal XlsxDocumentWriter(Stream xlsxStream, ILogger logger, bool ownsStream = false)
    {
        _writer = new(xlsxStream);
        _logger = logger;
        _ownedStream = ownsStream ? xlsxStream : null;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxDocumentWriter.AddSheet``1(System.String,System.Collections.Generic.IEnumerable{``0},System.Threading.CancellationToken)' />
    public void AddSheet<T>(string sheetName, IEnumerable<T> rows, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(rows);
        var properties = XlsxWriter.ReadableProperties<T>();
        WriteSheet(sheetName, XlsxWriter.HeaderNames(properties), XlsxWriter.RowsFromProperties(rows, properties), ct);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxDocumentWriter.AddSheet``1(System.String,System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.Threading.CancellationToken)' />
    public void AddSheet<T>(string sheetName, IEnumerable<T> rows, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(rows);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        WriteSheet(sheetName, XlsxWriter.HeaderNames(selectedProperties), XlsxWriter.RowsFromProperties(rows, selectedProperties), ct);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxDocumentWriter.AddSheetFromDataTable(System.String,Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public void AddSheetFromDataTable(string sheetName, DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        var (headers, rows) = XlsxWriter.BuildFromDataTable(dataTable);
        WriteSheet(sheetName, headers, rows, ct);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxDocumentWriter.AddSheetFromDictionary(System.String,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Threading.CancellationToken)' />
    public void AddSheetFromDictionary(string sheetName, IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var (headers, rows) = XlsxWriter.BuildFromDictionary(data, useHeaderRow);
        WriteSheet(sheetName, headers, rows, ct);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _writer.Dispose();
        _ownedStream?.Dispose();
    }

    private void WriteSheet(string sheetName, IReadOnlyList<string> headers, IEnumerable<XlsxCell[]> rows, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        if (_disposed)
            throw new ObjectDisposedException(nameof(XlsxDocumentWriter));

        if (!_sheetNames.Add(sheetName))
            throw new ArgumentException($"A sheet named '{sheetName}' has already been added to this document.", nameof(sheetName));

        _logger.LogDebug("Streaming worksheet {XlsxSheetName} into document session", sheetName);
        _writer.WriteSheet(sheetName, headers, rows, ct);
    }
}
