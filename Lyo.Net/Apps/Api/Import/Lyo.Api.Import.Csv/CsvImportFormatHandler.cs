using System.Text;
using System.Text.Json;
using Lyo.Api.Import;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Import.Csv;

/// <summary>CSV import. The file becomes a JSON array of objects, using the header row as property names. The JSON importer applies the field map.</summary>
public sealed class CsvImportFormatHandler : IImportFormatHandler
{
    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Csv;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        var rows = ReadRows(request.Content ?? string.Empty);
        if (rows is null)
            return ImportJsonConversion.Fail("CSV is empty.");

        return ImportJsonConversion.Ok(JsonSerializer.Serialize(rows));
    }

    private static List<Dictionary<string, string>>? ReadRows(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return null;

        var records = SplitRecords(csv);
        if (records.Count == 0 || records[0].Count == 0)
            return null;

        var headers = records[0];
        var rows = new List<Dictionary<string, string>>();
        for (var record = 1; record < records.Count; record++) {
            var fields = records[record];
            if (fields.Count == 1 && fields[0].Length == 0)
                continue;

            var row = new Dictionary<string, string>(headers.Count, StringComparer.Ordinal);
            for (var column = 0; column < headers.Count; column++) {
                var header = headers[column].Trim();
                if (header.Length == 0 || row.ContainsKey(header))
                    continue;

                row.Add(header, column < fields.Count ? fields[column] : string.Empty);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<List<string>> SplitRecords(string csv)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++) {
            var ch = csv[i];
            if (quoted) {
                if (ch != '"') {
                    field.Append(ch);
                    continue;
                }

                if (i + 1 < csv.Length && csv[i + 1] == '"') {
                    field.Append('"');
                    i++;
                    continue;
                }

                quoted = false;
                continue;
            }

            if (ch == '"') {
                quoted = true;
                continue;
            }

            if (ch == ',') {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (ch is '\n' or '\r') {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                row.Add(field.ToString());
                field.Clear();
                records.Add(row);
                row = [];
                continue;
            }

            field.Append(ch);
        }

        if (field.Length > 0 || row.Count > 0) {
            row.Add(field.ToString());
            records.Add(row);
        }

        return records;
    }
}
