using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;
#if NETSTANDARD2_0
#endif

namespace Lyo.Xlsx;

internal sealed class XlsxWriter : IXlsxWriter
{
    private readonly ILogger _logger;

    static XlsxWriter()
    {
        // Still required by the ClosedXML-backed import/DataTable read path when loading workbooks on non-Windows hosts.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans");
    }

    internal XlsxWriter(ILogger logger) => _logger = logger;

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
        var properties = ReadableProperties<T>();
        WriteToFile(xlsxFilePath, (writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(properties), RowsFromProperties(data, properties), ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream", typeof(T).FullName);
        var properties = ReadableProperties<T>();
        WriteToStream(xlsxStream, (writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(properties), RowsFromProperties(data, properties), ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes", typeof(T).FullName);
        var properties = ReadableProperties<T>();
        return WriteToBytes((writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(properties), RowsFromProperties(data, properties), ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath} with {PropertyCount} selected properties", typeof(T).FullName, xlsxFilePath, selectedProperties.Count);
        WriteToFile(xlsxFilePath, (writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        WriteToStream(xlsxStream, (writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        return WriteToBytes((writer, ct) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to {XlsxExportPath}", dataSets.Count, typeof(T).FullName, xlsxFilePath);
        WriteToFile(xlsxFilePath, (writer, ct) => WriteSheets(writer, dataSets, ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx stream", dataSets.Count, typeof(T).FullName);
        WriteToStream(xlsxStream, (writer, ct) => WriteSheets(writer, dataSets, ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytes``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}})' />
    public byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx bytes", dataSets.Count, typeof(T).FullName);
        return WriteToBytes((writer, ct) => WriteSheets(writer, dataSets, ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Boolean)' />
    public void ExportToXlsxFromDictionary(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting dictionary to {XlsxExportPath}", xlsxFilePath);
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        WriteToFile(xlsxFilePath, (writer, ct) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Boolean)' />
    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to xlsx stream");
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        WriteToStream(xlsxStream, (writer, ct) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, ct));
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean)' />
    public byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        return WriteToBytes((writer, ct) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.String)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting data table to {XlsxExportPath}", xlsxFilePath);
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        WriteToFile(xlsxFilePath, (writer, ct) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.IO.Stream)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting data table to xlsx stream");
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        WriteToStream(xlsxStream, (writer, ct) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesFromDataTable(Lyo.DataTable.Models.DataTable)' />
    public byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        return WriteToBytes((writer, ct) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, ct));
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.CreateDocumentWriter(System.IO.Stream)' />
    public IXlsxDocumentWriter CreateDocumentWriter(Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Opening xlsx document writing session on stream");
        return new XlsxDocumentWriter(xlsxStream, _logger);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.CreateDocumentWriter(System.String)' />
    public IXlsxDocumentWriter CreateDocumentWriter(string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Opening xlsx document writing session on {XlsxExportPath}", xlsxFilePath);
        var fileStream = File.Create(xlsxFilePath);
        try {
            return new XlsxDocumentWriter(fileStream, _logger, true);
        }
        catch {
            fileStream.Dispose();
            throw;
        }
    }

    private static void WriteToStream(Stream stream, Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct = default)
    {
        using var writer = new OpenXmlStreamWriter(stream);
        write(writer, ct);
    }

    private static void WriteToFile(string filePath, Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct = default)
    {
        using var fileStream = File.Create(filePath);
        WriteToStream(fileStream, write, ct);
    }

    private static byte[] WriteToBytes(Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        WriteToStream(memoryStream, write, ct);
        return memoryStream.ToArray();
    }

    private static void WriteSheets<T>(OpenXmlStreamWriter writer, IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct)
    {
        var properties = ReadableProperties<T>();
        var headers = HeaderNames(properties);
        foreach (var dataSet in dataSets) {
            ct.ThrowIfCancellationRequested();
            writer.WriteSheet(dataSet.Key, headers, RowsFromProperties(dataSet.Value, properties), ct);
        }
    }

    internal static IReadOnlyList<PropertyInfo> ReadableProperties<T>() => typeof(T).GetProperties().Where(p => p.CanRead).ToList();

    internal static List<string> HeaderNames(IReadOnlyList<PropertyInfo> properties)
    {
        var names = new List<string>(properties.Count);
        foreach (var property in properties)
            names.Add(property.Name);

        return names;
    }

    internal static IEnumerable<XlsxCell[]> RowsFromProperties<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> properties)
    {
        foreach (var item in data) {
            var cells = new XlsxCell[properties.Count];
            for (var c = 0; c < properties.Count; c++)
                cells[c] = ToCell(properties[c].GetValue(item));

            yield return cells;
        }
    }

    private static XlsxCell ToCell(object? value)
        => value switch {
            null => XlsxCell.Text(string.Empty),
            DateTime dateTime => XlsxCell.Date(dateTime),
            bool boolean => XlsxCell.Boolean(boolean),
            decimal or double or float or int or long => XlsxCell.Number(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
            var _ => XlsxCell.Text(value.ToString())
        };

    internal static (List<string> Headers, List<XlsxCell[]> Rows, XlsxCell[]? Footer) BuildFromDictionary(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool useHeaderRow,
        bool useFooterRow = false)
    {
        var headers = new List<string>();
        var rows = new List<XlsxCell[]>();
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        if (orderedRows.Count == 0)
            return (headers, rows, null);

        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        var dataStart = 0;
        if (useHeaderRow) {
            var firstRow = orderedRows[0].Value;
            for (var c = 0; c < maxCol; c++)
                headers.Add(firstRow.TryGetValue(c, out var value) ? value : "");

            dataStart = 1;
        }
        else {
            for (var c = 0; c < maxCol; c++)
                headers.Add($"Column{c}");
        }

        var dataEnd = orderedRows.Count;
        XlsxCell[]? footer = null;
        if (useFooterRow && dataEnd > dataStart) {
            var footerMap = orderedRows[dataEnd - 1].Value;
            footer = new XlsxCell[maxCol];
            for (var c = 0; c < maxCol; c++)
                footer[c] = XlsxCell.Text(footerMap.TryGetValue(c, out var value) ? value : "");

            dataEnd--;
        }

        for (var i = dataStart; i < dataEnd; i++) {
            var rowMap = orderedRows[i].Value;
            var cells = new XlsxCell[maxCol];
            for (var c = 0; c < maxCol; c++)
                cells[c] = XlsxCell.Text(rowMap.TryGetValue(c, out var value) ? value : "");

            rows.Add(cells);
        }

        return (headers, rows, footer);
    }

    internal static (List<string> Headers, List<XlsxCell[]> Rows, List<DataTableCellFormat?>? HeaderFormats, XlsxCell[]? Footer, List<DataTableCellFormat?>? FooterFormats)
        BuildFromDataTable(DataTable.Models.DataTable dataTable)
    {
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        var headers = new List<string>(maxCol);
        var hasFormats = dataTable.HasFormats;
        var headerFormats = hasFormats ? new List<DataTableCellFormat?>(maxCol) : null;
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            headers.Add(header?.DisplayValue ?? "");
            headerFormats?.Add(dataTable.GetFormat(-1, c));
        }

        var rows = new List<XlsxCell[]>();
        for (var rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++) {
            var row = dataTable.Rows[rowIndex];
            var cells = new XlsxCell[maxCol];
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells.TryGetValue(c, out var cellValue) ? cellValue : null;
                var xlsxCell = XlsxCell.Text(cell?.DisplayValue ?? "");
                if (cell != null && (cell.ColSpan > 1 || cell.RowSpan > 1))
                    xlsxCell = xlsxCell.WithSpan(cell.ColSpan, cell.RowSpan);

                if (hasFormats) {
                    var format = dataTable.GetFormat(rowIndex, c);
                    if (format != null)
                        xlsxCell = xlsxCell.WithFormat(format);
                }

                cells[c] = xlsxCell;
            }

            rows.Add(cells);
        }

        XlsxCell[]? footer = null;
        List<DataTableCellFormat?>? footerFormats = null;
        if (dataTable.Footer.Count > 0) {
            var orderedFooters = dataTable.Footer.OrderBy(kv => kv.Key).ToList();
            footer = new XlsxCell[maxCol];
            footerFormats = hasFormats ? new List<DataTableCellFormat?>(maxCol) : null;
            for (var c = 0; c < maxCol; c++) {
                var footerCell = orderedFooters.FirstOrDefault(f => f.Key == c).Value;
                var xlsxCell = XlsxCell.Text(footerCell?.DisplayValue ?? "");
                if (footerCell != null && (footerCell.ColSpan > 1 || footerCell.RowSpan > 1))
                    xlsxCell = xlsxCell.WithSpan(footerCell.ColSpan, footerCell.RowSpan);

                if (hasFormats) {
                    var format = dataTable.GetFormat(-2, c);
                    footerFormats!.Add(format);
                    if (format != null)
                        xlsxCell = xlsxCell.WithFormat(format);
                }

                footer[c] = xlsxCell;
            }
        }

        return (headers, rows, headerFormats, footer, footerFormats);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
        var properties = ReadableProperties<T>();
        var headers = HeaderNames(properties);
        await GuardAsync(() => RunToFileAsync(
                xlsxFilePath, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", headers, RowsFromProperties(data, properties), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream", typeof(T).FullName);
        var properties = ReadableProperties<T>();
        var headers = HeaderNames(properties);
        await GuardAsync(() => RunToStreamAsync(
                xlsxStream, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", headers, RowsFromProperties(data, properties), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes", typeof(T).FullName);
        var properties = ReadableProperties<T>();
        var headers = HeaderNames(properties);
        return await GuardAsync(() => RunToBytesAsync((writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", headers, RowsFromProperties(data, properties), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath} with {PropertyCount} selected properties", typeof(T).FullName, xlsxFilePath, selectedProperties.Count);
        await GuardAsync(() => RunToFileAsync(
                xlsxFilePath, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), token),
                ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await GuardAsync(() => RunToStreamAsync(
                xlsxStream, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), token),
                ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columns);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        var headers = new List<string>(columns.Count);
        var properties = new List<PropertyInfo>(columns.Count);
        foreach (var column in columns) {
            headers.Add(column.Key);
            properties.Add(column.Value);
        }

        await GuardAsync(() => RunToStreamAsync(
                xlsxStream, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", headers, RowsFromProperties(data, properties), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Func{``0,System.String}},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        var headers = new List<string>(columnFormatters.Count);
        var formatters = new List<Func<T, string>>(columnFormatters.Count);
        foreach (var formatter in columnFormatters) {
            headers.Add(formatter.Key);
            formatters.Add(formatter.Value);
        }

        await GuardAsync(() => RunToStreamAsync(
                xlsxStream, (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", headers, RowsFromFormatters(data, formatters), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        return await GuardAsync(() => RunToBytesAsync(
                (writer, token) => writer.WriteSheet(worksheetName ?? "Sheet1", HeaderNames(selectedProperties), RowsFromProperties(data, selectedProperties), token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to {XlsxExportPath}", dataSets.Count, typeof(T).FullName, xlsxFilePath);
        await GuardAsync(() => RunToFileAsync(xlsxFilePath, (writer, token) => WriteSheets(writer, dataSets, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx stream", dataSets.Count, typeof(T).FullName);
        await GuardAsync(() => RunToStreamAsync(xlsxStream, (writer, token) => WriteSheets(writer, dataSets, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx bytes", dataSets.Count, typeof(T).FullName);
        return await GuardAsync(() => RunToBytesAsync((writer, token) => WriteSheets(writer, dataSets, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        await GuardAsync(() => RunToFileAsync(xlsxFilePath, (writer, token) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        await GuardAsync(() => RunToStreamAsync(xlsxStream, (writer, token) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool useHeaderRow = true,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var (headers, rows, footer) = BuildFromDictionary(data, useHeaderRow, useFooterRow);
        return await GuardAsync(() => RunToBytesAsync((writer, token) => writer.WriteSheet("Sheet1", headers, rows, null, footer, null, token), ct)).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        await GuardAsync(() => RunToFileAsync(xlsxFilePath, (writer, token) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        await GuardAsync(() => RunToStreamAsync(xlsxStream, (writer, token) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        var (headers, rows, headerFormats, footer, footerFormats) = BuildFromDataTable(dataTable);
        return await GuardAsync(() => RunToBytesAsync((writer, token) => writer.WriteSheet("Sheet1", headers, rows, headerFormats, footer, footerFormats, token), ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IAsyncEnumerable{``0},System.String,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IAsyncEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var list = await MaterializeAsync(data, ct).ConfigureAwait(false);
        await ExportToXlsxAsync(list, xlsxFilePath, worksheetName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxAsync``1(System.Collections.Generic.IAsyncEnumerable{``0},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IAsyncEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var list = await MaterializeAsync(data, ct).ConfigureAwait(false);
        await ExportToXlsxAsync(list, xlsxStream, worksheetName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxWriter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IAsyncEnumerable{``0},System.String,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(IAsyncEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        var list = await MaterializeAsync(data, ct).ConfigureAwait(false);
        return await ExportToXlsxBytesAsync(list, worksheetName, ct).ConfigureAwait(false);
    }

    private static async Task<List<T>> MaterializeAsync<T>(IAsyncEnumerable<T> data, CancellationToken ct)
    {
        var list = new List<T>();
        await foreach (var item in data.WithCancellation(ct).ConfigureAwait(false))
            list.Add(item);

        return list;
    }

    // Throw synchronously so an already-cancelled token surfaces the exact OperationCanceledException (not a derived TaskCanceledException).
    private static Task RunToStreamAsync(Stream stream, Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => WriteToStream(stream, write, ct), ct);
    }

    private static Task RunToFileAsync(string filePath, Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => WriteToFile(filePath, write, ct), ct);
    }

    private static Task<byte[]> RunToBytesAsync(Action<OpenXmlStreamWriter, CancellationToken> write, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => WriteToBytes(write, ct), ct);
    }

    private async Task GuardAsync(Func<Task> action)
    {
        try {
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("XLSX export operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "XLSX export operation failed");
            throw;
        }
    }

    private async Task<TResult> GuardAsync<TResult>(Func<Task<TResult>> action)
    {
        try {
            return await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("XLSX export operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "XLSX export operation failed");
            throw;
        }
    }

    private static IEnumerable<XlsxCell[]> RowsFromFormatters<T>(IEnumerable<T> data, IReadOnlyList<Func<T, string>> formatters)
    {
        foreach (var item in data) {
            var cells = new XlsxCell[formatters.Count];
            for (var c = 0; c < formatters.Count; c++)
                cells[c] = XlsxCell.Text(formatters[c](item));

            yield return cells;
        }
    }
#endif
}