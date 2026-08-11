using System.Globalization;
using System.Text.Json;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Xlsx;
using Lyo.Xlsx.Models;
using DataTableModel = Lyo.DataTable.Models.DataTable;

namespace Lyo.Cli.Services;

/// <summary>XLSX merge/split/sheets/stats wrappers.</summary>
internal static class CliXlsx
{
    private const int SampleRowLimit = 5;

    public static XlsxService Create() => new();

    public static IReadOnlyList<string> ListSheets(string file, XlsxService xlsx)
        => xlsx.ListSheetNames(file);

    public static void Merge(IEnumerable<string> files, string output, XlsxMergeMode mode, XlsxService xlsx)
    {
        var list = files.ToArray();
        ArgumentHelpers.ThrowIf(list.Length == 0, "At least one input file is required.");
        xlsx.MergeXlsxFiles(list, output, mode);
    }

    public static IReadOnlyList<string> Split(string file, string by, int? rows, string dir, string? sheet, XlsxService xlsx)
    {
        Directory.CreateDirectory(dir);
        return by.Trim().ToLowerInvariant() switch {
            "sheet" or "sheets" => xlsx.SplitXlsxBySheet(file, dir),
            "rows" or "row" => xlsx.SplitXlsxByRows(file, rows ?? throw new ArgumentException("--rows is required when --by rows."), dir, sheet),
            var _ => throw new ArgumentException($"Unknown --by '{by}'. Use sheet or rows.")
        };
    }

    public static void ToCsv(string input, string output, string? sheet, XlsxService xlsx)
    {
        if (string.IsNullOrWhiteSpace(sheet)) {
            xlsx.ConvertXlsxToCsv(input, output);
            return;
        }

        CliTabularConvert.XlsxSheetToCsv(input, output, sheet, xlsx);
    }

    /// <summary>
    /// Workbook statistics as JSON (file size, sheet list, per-sheet row/column/header/sample/types).
    /// Optional <paramref name="sheet" /> limits output to one sheet (name or 0-based index).
    /// </summary>
    public static string Stats(string? input, string? sheet, bool? hasHeader, XlsxService xlsx)
    {
        long? fileSize = null;
        IReadOnlyDictionary<string, DataTableModel> sheets;
        IReadOnlyList<string> sheetNames;

        if (!string.IsNullOrWhiteSpace(input) && input != "-") {
            ArgumentHelpers.ThrowIf(!File.Exists(input), $"Input file not found: {input}");
            fileSize = new FileInfo(input).Length;
            sheetNames = xlsx.ListSheetNames(input);
            sheets = xlsx.ParseXlsxFileAsAllSheets(input, hasHeader);
        }
        else {
            var (stream, leaveOpen, _) = CliIO.OpenInput(input);
            try {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                fileSize = bytes.LongLength;
                sheetNames = xlsx.ListSheetNames(bytes);
                sheets = xlsx.ParseXlsxBytesAsAllSheets(bytes, hasHeader);
            }
            finally {
                if (!leaveOpen)
                    stream.Dispose();
            }
        }

        if (!string.IsNullOrWhiteSpace(sheet)) {
            var one = ResolveSheet(sheets, sheetNames, sheet);
            var payload = new {
                FileSizeBytes = fileSize,
                SheetCount = 1,
                SheetNames = new[] { one.Name },
                Sheets = new[] { one }
            };
            return JsonSerializer.Serialize(payload, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true));
        }

        var sheetStats = sheetNames
            .Select(name => sheets.TryGetValue(name, out var dt)
                ? BuildSheetStats(name, dt)
                : BuildSheetStats(name, new DataTableModel()))
            .ToArray();

        var all = new {
            FileSizeBytes = fileSize,
            SheetCount = sheetNames.Count,
            SheetNames = sheetNames,
            Sheets = sheetStats
        };
        return JsonSerializer.Serialize(all, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true));
    }

    public static XlsxMergeMode ParseMode(string? mode)
        => (mode ?? "preserve").Trim().ToLowerInvariant() switch {
            "preserve" or "sheets" => XlsxMergeMode.PreserveSheets,
            "concat" or "concatenate" or "rows" => XlsxMergeMode.ConcatenateRows,
            var _ => throw new ArgumentException($"Unknown merge mode '{mode}'. Use preserve or concat.")
        };

    private static SheetStats ResolveSheet(
        IReadOnlyDictionary<string, DataTableModel> sheets,
        IReadOnlyList<string> sheetNames,
        string sheet)
    {
        if (int.TryParse(sheet, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)) {
            ArgumentHelpers.ThrowIf(index < 0 || index >= sheetNames.Count, $"Sheet index {index} out of range (0..{sheetNames.Count - 1}).");
            var byIndex = sheetNames[index];
            return BuildSheetStats(byIndex, sheets[byIndex]);
        }

        ArgumentHelpers.ThrowIf(!sheets.TryGetValue(sheet, out var table), $"Sheet '{sheet}' not found. Available: {string.Join(", ", sheetNames)}");
        return BuildSheetStats(sheet, table!);
    }

    private static SheetStats BuildSheetStats(string name, DataTableModel table)
    {
        var colIndexes = table.Headers.Count > 0
            ? table.Headers.Keys.OrderBy(i => i).ToArray()
            : table.Rows.SelectMany(r => r.Cells.Keys).Distinct().OrderBy(i => i).ToArray();

        var headers = new List<string>(colIndexes.Length);
        foreach (var col in colIndexes) {
            var header = table.Headers.TryGetValue(col, out var cell) ? cell.DisplayValue : null;
            headers.Add(string.IsNullOrWhiteSpace(header) ? $"Column{col}" : header!);
        }

        var inferred = new Dictionary<string, string>(StringComparer.Ordinal);
        var samples = new List<Dictionary<string, string>>(SampleRowLimit);
        var take = Math.Min(SampleRowLimit, table.Rows.Count);
        for (var r = 0; r < take; r++) {
            var row = table.Rows[r];
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < colIndexes.Length; i++) {
                var col = colIndexes[i];
                var header = headers[i];
                var value = row[col].DisplayValue ?? string.Empty;
                dict[header] = value;
                var key = i.ToString(CultureInfo.InvariantCulture);
                if (!inferred.ContainsKey(key) && !string.IsNullOrWhiteSpace(value))
                    inferred[key] = InferTypeName(value);
            }

            samples.Add(dict);
        }

        if (inferred.Count < colIndexes.Length) {
            for (var r = take; r < table.Rows.Count && inferred.Count < colIndexes.Length; r++) {
                var row = table.Rows[r];
                for (var i = 0; i < colIndexes.Length; i++) {
                    var key = i.ToString(CultureInfo.InvariantCulture);
                    if (inferred.ContainsKey(key))
                        continue;
                    var value = row[colIndexes[i]].DisplayValue ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(value))
                        inferred[key] = InferTypeName(value);
                }
            }
        }

        for (var i = 0; i < colIndexes.Length; i++) {
            var key = i.ToString(CultureInfo.InvariantCulture);
            if (!inferred.ContainsKey(key))
                inferred[key] = nameof(String);
        }

        return new SheetStats(
            name,
            table.Rows.Count,
            colIndexes.Length,
            headers,
            inferred,
            table.Footer.Count > 0,
            samples);
    }

    private static string InferTypeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return nameof(String);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return nameof(Int32);
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            return nameof(Decimal);
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out _))
            return nameof(DateTime);
        return nameof(String);
    }

    private sealed record SheetStats(
        string Name,
        long RowCount,
        int ColumnCount,
        IReadOnlyList<string> Headers,
        IReadOnlyDictionary<string, string> InferredColumnTypes,
        bool HasFooterRow,
        IReadOnlyList<Dictionary<string, string>> SampleRows);
}
