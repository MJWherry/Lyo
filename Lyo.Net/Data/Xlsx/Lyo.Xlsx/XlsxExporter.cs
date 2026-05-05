using System.Reflection;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx;

internal sealed class XlsxExporter : IXlsxExporter
{
    private readonly ILogger _logger;

    static XlsxExporter()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans");
    }

    internal XlsxExporter(ILogger logger) => _logger = logger;

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
        using var workbook = CreateWorkbookFromData(data, worksheetName);
        workbook.SaveAs(xlsxFilePath);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream", typeof(T).FullName);
        using var workbook = CreateWorkbookFromData(data, worksheetName);
        workbook.SaveAs(xlsxStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes", typeof(T).FullName);
        using var memoryStream = new MemoryStream();
        ExportToXlsx(data, memoryStream, worksheetName);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath} with {PropertyCount} selected properties", typeof(T).FullName, xlsxFilePath, selectedProperties.Count);
        using var workbook = CreateWorkbookFromData(data, selectedProperties, worksheetName);
        workbook.SaveAs(xlsxFilePath);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var workbook = CreateWorkbookFromData(data, selectedProperties, worksheetName);
        workbook.SaveAs(xlsxStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var memoryStream = new MemoryStream();
        ExportToXlsx(data, selectedProperties, memoryStream, worksheetName);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to {XlsxExportPath}", dataSets.Count, typeof(T).FullName, xlsxFilePath);
        using var workbook = new XLWorkbook();
        foreach (var dataSet in dataSets) {
            var worksheet = workbook.Worksheets.Add(dataSet.Key);
            WriteDataToWorksheet(dataSet.Value, worksheet);
        }

        workbook.SaveAs(xlsxFilePath);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx stream", dataSets.Count, typeof(T).FullName);
        using var workbook = new XLWorkbook();
        foreach (var dataSet in dataSets) {
            var worksheet = workbook.Worksheets.Add(dataSet.Key);
            WriteDataToWorksheet(dataSet.Value, worksheet);
        }

        workbook.SaveAs(xlsxStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytes``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}})' />
    public byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx bytes", dataSets.Count, typeof(T).FullName);
        using var memoryStream = new MemoryStream();
        ExportToXlsx(dataSets, memoryStream);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean)' />
    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting dictionary to {XlsxExportPath}", xlsxFilePath);
        using var workbook = CreateWorkbookFromDictionary(data, useHeaderRow);
        workbook.SaveAs(xlsxFilePath);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean)' />
    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to xlsx stream");
        using var workbook = CreateWorkbookFromDictionary(data, useHeaderRow);
        workbook.SaveAs(xlsxStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean)' />
    public byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data);
        using var ms = new MemoryStream();
        ExportToXlsxFromDictionary(data, ms, useHeaderRow);
        return ms.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.String)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting data table to {XlsxExportPath}", xlsxFilePath);
        using var workbook = CreateWorkbookFromDataTable(dataTable);
        workbook.SaveAs(xlsxFilePath);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.IO.Stream)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting data table to xlsx stream");
        using var workbook = CreateWorkbookFromDataTable(dataTable);
        workbook.SaveAs(xlsxStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesFromDataTable(Lyo.DataTable.Models.DataTable)' />
    public byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        using var ms = new MemoryStream();
        ExportToXlsxFromDataTable(dataTable, ms);
        return ms.ToArray();
    }

    private static XLWorkbook CreateWorkbookFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        if (orderedRows.Count == 0)
            return workbook;

        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        if (useHeaderRow && orderedRows.Count > 0) {
            var firstRow = orderedRows[0].Value;
            for (var c = 0; c < maxCol; c++)
                worksheet.Cell(1, c + 1).Value = firstRow.TryGetValue(c, out var v) ? v ?? "" : "";

            worksheet.Row(1).Style.Font.Bold = true;
            orderedRows = orderedRows.Skip(1).ToList();
        }
        else {
            for (var c = 0; c < maxCol; c++)
                worksheet.Cell(1, c + 1).Value = $"Column{c}";

            worksheet.Row(1).Style.Font.Bold = true;
        }

        var rowNum = 2;
        foreach (var kv in orderedRows) {
            for (var c = 0; c < maxCol; c++)
                worksheet.Cell(rowNum, c + 1).Value = kv.Value.TryGetValue(c, out var v) ? v ?? "" : "";

            rowNum++;
        }

        worksheet.Columns().AdjustToContents();
        return workbook;
    }

    private static XLWorkbook CreateWorkbookFromDataTable(DataTable.Models.DataTable dataTable)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            worksheet.Cell(1, c + 1).Value = header?.DisplayValue ?? "";
        }

        worksheet.Row(1).Style.Font.Bold = true;
        var rowNum = 2;
        foreach (var row in dataTable.Rows) {
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells.TryGetValue(c, out var cellVal) ? cellVal : null;
                worksheet.Cell(rowNum, c + 1).Value = cell?.DisplayValue ?? "";
            }

            rowNum++;
        }

        worksheet.Columns().AdjustToContents();
        return workbook;
    }

    private XLWorkbook CreateWorkbookFromData<T>(IEnumerable<T> data, string? worksheetName = null)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName ?? "Sheet1");
        WriteDataToWorksheet(data, worksheet);
        return workbook;
    }

    private XLWorkbook CreateWorkbookFromData<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName ?? "Sheet1");
        WriteDataToWorksheet(data, selectedProperties, worksheet);
        return workbook;
    }

    private XLWorkbook CreateWorkbookFromData<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, string? worksheetName = null)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName ?? "Sheet1");
        WriteDataToWorksheet(data, columns, worksheet);
        return workbook;
    }

    private XLWorkbook CreateWorkbookFromData<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, string? worksheetName = null)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName ?? "Sheet1");
        WriteDataToWorksheet(data, columnFormatters, worksheet);
        return workbook;
    }

    private void WriteDataToWorksheet<T>(IEnumerable<T> data, IXLWorksheet worksheet)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return;

        var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToList();
        WriteDataToWorksheet(dataList, properties, worksheet);
    }

    private void WriteDataToWorksheet<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, IXLWorksheet worksheet)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return;

        WriteDataToWorksheet(dataList, selectedProperties, worksheet);
    }

    private void WriteDataToWorksheet<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, IXLWorksheet worksheet)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return;

        WriteDataToWorksheet(dataList, columns, worksheet);
    }

    private void WriteDataToWorksheet<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, IXLWorksheet worksheet)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return;

        WriteDataToWorksheet(dataList, columnFormatters, worksheet);
    }

    private static void WriteDataToWorksheet<T>(List<T> dataList, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, IXLWorksheet worksheet)
    {
        var colIndex = 0;
        foreach (var header in columnFormatters.Keys) {
            worksheet.Cell(1, colIndex + 1).Value = header;
            worksheet.Cell(1, colIndex + 1).Style.Font.Bold = true;
            colIndex++;
        }

        var formatters = columnFormatters.Values.ToList();
        var row = 2;
        foreach (var item in dataList) {
            for (var col = 0; col < formatters.Count; col++) {
                var value = formatters[col](item);
                worksheet.Cell(row, col + 1).Value = value ?? "";
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void WriteDataToWorksheet<T>(List<T> dataList, IReadOnlyDictionary<string, PropertyInfo> columns, IXLWorksheet worksheet)
    {
        var colIndex = 0;
        foreach (var header in columns.Keys) {
            worksheet.Cell(1, colIndex + 1).Value = header;
            worksheet.Cell(1, colIndex + 1).Style.Font.Bold = true;
            colIndex++;
        }

        var properties = columns.Values.ToList();
        var row = 2;
        foreach (var item in dataList) {
            for (var col = 0; col < properties.Count; col++) {
                var value = properties[col].GetValue(item);
                if (value != null) {
                    switch (value) {
                        case DateTime dateTime:
                            worksheet.Cell(row, col + 1).Value = dateTime;
                            worksheet.Cell(row, col + 1).Style.DateFormat.Format = "mm/dd/yyyy";
                            break;
                        case decimal or double or float or int or long:
                            worksheet.Cell(row, col + 1).Value = Convert.ToDouble(value);
                            break;
                        case bool boolean:
                            worksheet.Cell(row, col + 1).Value = boolean;
                            break;
                        default:
                            worksheet.Cell(row, col + 1).Value = value.ToString();
                            break;
                    }
                }
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void WriteDataToWorksheet<T>(List<T> dataList, IReadOnlyList<PropertyInfo> properties, IXLWorksheet worksheet)
    {
        for (var i = 0; i < properties.Count; i++) {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in dataList) {
            for (var col = 0; col < properties.Count; col++) {
                var value = properties[col].GetValue(item);
                if (value != null) {
                    switch (value) {
                        case DateTime dateTime:
                            worksheet.Cell(row, col + 1).Value = dateTime;
                            worksheet.Cell(row, col + 1).Style.DateFormat.Format = "mm/dd/yyyy";
                            break;
                        case decimal or double or float or int or long:
                            worksheet.Cell(row, col + 1).Value = Convert.ToDouble(value);
                            break;
                        case bool boolean:
                            worksheet.Cell(row, col + 1).Value = boolean;
                            break;
                        default:
                            worksheet.Cell(row, col + 1).Value = value.ToString();
                            break;
                    }
                }
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxFilePath);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for {XlsxExportPath}", xlsxFilePath);
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx stream", typeof(T).FullName);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxStream);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for stream");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to stream", typeof(T).FullName);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes", typeof(T).FullName);
        await using var memoryStream = new MemoryStream();
        await ExportToXlsxAsync(data, memoryStream, worksheetName, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String,System.Threading.CancellationToken)' />
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
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, selectedProperties, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxFilePath);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for {XlsxExportPath}", xlsxFilePath);
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to {XlsxExportPath}", typeof(T).FullName, xlsxFilePath);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
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
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, selectedProperties, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxStream);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for stream");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to stream", typeof(T).FullName);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
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
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, columns, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxStream);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for stream");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to stream", typeof(T).FullName);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Func{``0,System.String}},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
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
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = CreateWorkbookFromData(data, columnFormatters, worksheetName);
                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxStream);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for stream");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxExportType} to stream", typeof(T).FullName);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string? worksheetName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(selectedProperties);
        _logger.LogDebug("Exporting {XlsxExportType} to xlsx bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var memoryStream = new MemoryStream();
        await ExportToXlsxAsync(data, selectedProperties, memoryStream, worksheetName, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to {XlsxExportPath}", dataSets.Count, typeof(T).FullName, xlsxFilePath);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = new XLWorkbook();
                        foreach (var dataSet in dataSets) {
                            ct.ThrowIfCancellationRequested();
                            var worksheet = workbook.Worksheets.Add(dataSet.Key);
                            WriteDataToWorksheet(dataSet.Value, worksheet);
                        }

                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxFilePath);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for {XlsxExportPath}", xlsxFilePath);
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxSheetCount} sheets to {XlsxExportPath}", dataSets.Count, xlsxFilePath);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotWritable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be writable.");
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx stream", dataSets.Count, typeof(T).FullName);
        ct.ThrowIfCancellationRequested();
        try {
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = new XLWorkbook();
                        foreach (var dataSet in dataSets) {
                            ct.ThrowIfCancellationRequested();
                            var worksheet = workbook.Worksheets.Add(dataSet.Key);
                            WriteDataToWorksheet(dataSet.Value, worksheet);
                        }

                        ct.ThrowIfCancellationRequested();
                        workbook.SaveAs(xlsxStream);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Export operation was cancelled for stream");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export {XlsxSheetCount} sheets to stream", dataSets.Count);
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataSets);
        _logger.LogDebug("Exporting {XlsxSheetCount} sheets of {XlsxExportType} to xlsx bytes", dataSets.Count, typeof(T).FullName);
        await using var memoryStream = new MemoryStream();
        await ExportToXlsxAsync(dataSets, memoryStream, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        await Task.Run(() => ExportToXlsxFromDictionary(data, xlsxFilePath, useHeaderRow), ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        await Task.Run(() => ExportToXlsxFromDictionary(data, xlsxStream, useHeaderRow), ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool useHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return await Task.Run(() => ExportToXlsxBytesFromDictionary(data, useHeaderRow), ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xlsxFilePath);
        await Task.Run(() => ExportToXlsxFromDataTable(dataTable, xlsxFilePath), ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        await Task.Run(() => ExportToXlsxFromDataTable(dataTable, xlsxStream), ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxExporter.ExportToXlsxBytesFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        return await Task.Run(() => ExportToXlsxBytesFromDataTable(dataTable), ct).ConfigureAwait(false);
    }
#endif
}