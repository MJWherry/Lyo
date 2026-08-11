using Lyo.Csv;
using Lyo.Exceptions;
using Lyo.Xlsx;

namespace Lyo.Cli.Services;

/// <summary>DataTable-mediated csv↔xlsx conversion (Gateway workbench pattern).</summary>
internal static class CliTabularConvert
{
    public static async Task CsvToXlsxAsync(string? input, string output, CsvService csv, XlsxService xlsx, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(output);
        if (!string.IsNullOrWhiteSpace(input) && input != "-") {
            var parsed = await csv.ParseFileAsDataTableAsync(input, ct: ct).ConfigureAwait(false);
            ArgumentHelpers.ThrowIf(!parsed.IsSuccess, parsed.Errors is { Count: > 0 } ? parsed.Errors[0].Message : "Failed to parse CSV.");
            await xlsx.ExportToXlsxFromDataTableAsync(parsed.Data!, output, ct).ConfigureAwait(false);
            return;
        }

        var (stream, leaveOpen, _) = CliIO.OpenInput(input);
        try {
            var parsed = await csv.ParseStreamAsDataTableAsync(stream, ct: ct).ConfigureAwait(false);
            ArgumentHelpers.ThrowIf(!parsed.IsSuccess, parsed.Errors is { Count: > 0 } ? parsed.Errors[0].Message : "Failed to parse CSV.");
            await xlsx.ExportToXlsxFromDataTableAsync(parsed.Data!, output, ct).ConfigureAwait(false);
        }
        finally {
            if (!leaveOpen)
                await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static void XlsxSheetToCsv(string input, string output, string sheet, XlsxService xlsx)
    {
        var csv = CliCsv.Create();
        var parsed = xlsx.ParseXlsxFileAsDataTable(input, sheet);
        ArgumentHelpers.ThrowIf(!parsed.IsSuccess, parsed.Errors is { Count: > 0 } ? parsed.Errors[0].Message : "Failed to parse XLSX sheet.");
        csv.ExportToCsvFromDataTable(parsed.Data!, output);
    }
}
