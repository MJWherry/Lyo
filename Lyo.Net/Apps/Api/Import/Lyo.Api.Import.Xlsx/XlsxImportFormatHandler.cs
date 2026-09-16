using System.Text.Json;
using ClosedXML.Excel;
using Lyo.Api.Import;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Import.Xlsx;

/// <summary>XLSX import. The worksheet becomes a JSON array of objects, using the header row as property names. The JSON importer applies the field map.</summary>
public sealed class XlsxImportFormatHandler : IImportFormatHandler
{
    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Xlsx;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        try {
            var bytes = request.ContentBytes;
            if ((bytes is null || bytes.Length == 0) && !string.IsNullOrWhiteSpace(request.Content))
                bytes = System.Convert.FromBase64String(request.Content);

            if (bytes is null || bytes.Length == 0)
                return ImportJsonConversion.Fail("XLSX is empty.");

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var sheet = string.IsNullOrWhiteSpace(request.SheetName)
                ? workbook.Worksheets.FirstOrDefault()
                : workbook.Worksheets.FirstOrDefault(item => string.Equals(item.Name, request.SheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet is null)
                return ImportJsonConversion.Fail(string.IsNullOrWhiteSpace(request.SheetName) ? "Workbook has no worksheets." : $"Worksheet '{request.SheetName}' was not found.");

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow == 0 || lastColumn == 0)
                return ImportJsonConversion.Fail("XLSX is empty.");

            var headers = new string[lastColumn];
            for (var column = 1; column <= lastColumn; column++)
                headers[column - 1] = sheet.Cell(1, column).GetString().Trim();

            var rows = new List<Dictionary<string, string>>();
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++) {
                var row = new Dictionary<string, string>(lastColumn, StringComparer.Ordinal);
                for (var column = 1; column <= lastColumn; column++) {
                    var header = headers[column - 1];
                    if (header.Length == 0 || row.ContainsKey(header))
                        continue;

                    row.Add(header, sheet.Cell(rowNumber, column).GetString());
                }

                rows.Add(row);
            }

            return ImportJsonConversion.Ok(JsonSerializer.Serialize(rows));
        }
        catch (Exception ex) {
            return ImportJsonConversion.Fail(ex.Message);
        }
    }
}
