using System.Text;
using System.Text.Json;
using Lyo.Common;
using Lyo.Csv;
using Lyo.Csv.Models;
using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>CSV merge/split/stats/validate wrappers.</summary>
internal static class CliCsv
{
    public static CsvService Create(char? delimiter = null, char? quote = null, string? encodingName = null, bool? hasHeader = null)
    {
        var options = new CsvOptions();
        if (delimiter is not null)
            options.Delimiter = delimiter.Value.ToString();

        if (quote is not null)
            options.Quote = quote.Value;

        if (!string.IsNullOrWhiteSpace(encodingName))
            options.Encoding = Encoding.GetEncoding(encodingName);

        if (hasHeader is not null)
            options.HasHeaderRecord = hasHeader.Value;

        return new(options: options);
    }

    public static async Task MergeAsync(IEnumerable<string> files, string output, bool includeHeaders, CsvService csv, CancellationToken ct)
    {
        var list = files.ToArray();
        ArgumentHelpers.ThrowIf(list.Length == 0, "At least one input file is required.");
        await csv.CombineCsvFilesAsync(list, output, includeHeaders, ct).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<string>> SplitAsync(string file, int rows, string dir, CsvService csv, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIf(rows <= 0, "--rows must be > 0.");
        Directory.CreateDirectory(dir);
        return await csv.SplitCsvFileAsync(file, rows, dir, ct).ConfigureAwait(false);
    }

    public static async Task AppendAsync(string target, string rowsFile, CsvService csv, CancellationToken ct)
    {
        var parsed = await csv.ParseFileAsDataTableAsync(rowsFile, ct: ct).ConfigureAwait(false);
        ArgumentHelpers.ThrowIf(!parsed.IsSuccess, parsed.Errors is { Count: > 0 } ? parsed.Errors[0].Message : "Failed to parse rows file.");
        var table = parsed.Data!;
        var rows = new List<Dictionary<string, object?>>();
        foreach (var row in table.Rows) {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (col, header) in table.Headers.OrderBy(kv => kv.Key)) {
                var name = string.IsNullOrWhiteSpace(header.DisplayValue) ? $"Column{col}" : header.DisplayValue;
                dict[name] = row[col].DisplayValue;
            }

            rows.Add(dict);
        }

        await csv.AppendToCsvAsync(rows, target, true, ct).ConfigureAwait(false);
    }

    public static async Task<string> StatsAsync(string? input, CsvService csv, CancellationToken ct)
    {
        CsvStatistics stats;
        if (!string.IsNullOrWhiteSpace(input) && input != "-")
            stats = await csv.GetStatisticsAsync(input, ct).ConfigureAwait(false);
        else {
            var (stream, leaveOpen, _) = CliIO.OpenInput(input);
            try {
                stats = await csv.GetStatisticsAsync(stream, ct).ConfigureAwait(false);
            }
            finally {
                if (!leaveOpen)
                    await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        // CsvStatistics holds Encoding / Type — not System.Text.Json-safe; project to primitives.
        var payload = new {
            stats.RowCount,
            stats.ColumnCount,
            stats.Headers,
            InferredColumnTypes = stats.InferredColumnTypes.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.Name),
            stats.FileSizeBytes,
            DetectedEncoding = stats.DetectedEncoding.WebName,
            DetectedDelimiter = stats.DetectedDelimiter?.ToString(),
            stats.HasHeaderRow,
            stats.SampleRows
        };

        return JsonSerializer.Serialize(payload, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true));
    }

    public static async Task<int> ValidateAsync(string? input, CsvService csv, CancellationToken ct)
    {
        var schema = new CsvSchema { RequireAllColumns = false, AllowExtraColumns = true };
        ValidationResult result;
        if (!string.IsNullOrWhiteSpace(input) && input != "-")
            result = await csv.ValidateAsync(input, schema, ct).ConfigureAwait(false);
        else {
            var (stream, leaveOpen, _) = CliIO.OpenInput(input);
            try {
                result = await csv.ValidateAsync(stream, schema, ct).ConfigureAwait(false);
            }
            finally {
                if (!leaveOpen)
                    await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        if (result.IsValid) {
            await Console.Out.WriteLineAsync("valid").ConfigureAwait(false);
            return 0;
        }

        await Console.Error.WriteLineAsync(JsonSerializer.Serialize(result, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true))).ConfigureAwait(false);
        return 1;
    }
}