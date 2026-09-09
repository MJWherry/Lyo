using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Pathing;
using Lyo.Common.Metadata.Records;
using Lyo.DataTable.Models;
using Lyo.Images;
using Lyo.Result;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Services;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.Models;
using Lyo.Xlsx.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class CsvXlsxWorkbench
{
    private const long MaxUploadBytes = 16 * 1024 * 1024;
    private const int MaxCsvTextEditBytes = 256 * 1024;

    /// <summary>Starting tab — <c>csv</c>, <c>xlsx</c>, <c>split</c>, <c>merge</c>, or <c>more</c>.</summary>
    [Parameter]
    public string InitialTab { get; set; } = "csv";

    private int _tabIndex;
    private bool _csvHasHeader = true;
    private string _csvText = "Name,Email\nAlice,alice@example.com\nBob,bob@example.com";
    private string _csvError = string.Empty;
    private string _csvUploadName = string.Empty;
    private byte[]? _csvBytes;
    private Lyo.DataTable.Models.DataTable? _csvTable;
    private bool _csvTextEditDisabled;

    private bool _xlsxUseHeaderRow = true;
    private string _xlsxError = string.Empty;
    private string _xlsxUploadName = string.Empty;
    private long _xlsxByteLength;
    private byte[]? _xlsxBytes;
    private Lyo.DataTable.Models.DataTable? _xlsxTable;

    private bool _mergeHeaderA = true;
    private bool _mergeHeaderB = true;
    private bool _mergeSkipFirstRowB = true;
    private string _mergeError = string.Empty;
    private string _mergeNameA = string.Empty;
    private string _mergeNameB = string.Empty;
    private byte[]? _mergeBytesA;
    private byte[]? _mergeBytesB;
    private Lyo.DataTable.Models.DataTable? _mergedTable;
    private byte[]? _mergedCombinedBytes;
    private bool _mergedCombinedIsXlsx;
    private XlsxMergeMode _xlsxMergeMode = XlsxMergeMode.PreserveSheets;

    private readonly List<LocalBrowserFile> _multiCombineFiles = [];

    private bool _multiCombineAllXlsx => _multiCombineFiles.Count > 0 && _multiCombineFiles.All(f => IsXlsx(f.FileName));

    private bool _multiCombineAllCsv => _multiCombineFiles.Count > 0 && _multiCombineFiles.All(f => IsCsv(f.FileName));

    private byte[]? _splitBytes;
    private string _splitFileName = string.Empty;
    private bool _splitIsXlsx;
    private string _splitXlsxMode = "sheet";
    private int _splitRowsPerFile = 1000;
    private string _splitSheetName = string.Empty;
    private int _splitPartCount;
    private string _splitError = string.Empty;

    private string _convertXlsxName = string.Empty;
    private byte[]? _convertXlsxBytes;

    private JsonNode? _dictJsonRoot;
    private string? _dictJsonError;
    private byte[]? _dictJsonBytes;
    private string _dictJsonFileName = "grid.json";
    private int _dictJsonViewerKey;

    protected override void OnInitialized()
        => _tabIndex = InitialTab switch {
            "xlsx" => 1,
            "split" => 2,
            "merge" => 3,
            "more" => 4,
            var _ => 0
        };

    private Task OnCsvHasHeaderChangedAsync()
    {
        if (_csvBytes is not { Length: > 0 } && string.IsNullOrEmpty(_csvText))
            return Task.CompletedTask;

        return _csvTextEditDisabled ? ParseCsvBytesAsync() : ParseCsvTextAsync();
    }

    private Task OnCsvUploadReadyAsync(LocalBrowserFile file)
    {
        _csvUploadName = file.FileName;
        _csvBytes = file.Content;
        if (file.Content.Length > MaxCsvTextEditBytes) {
            _csvTextEditDisabled = true;
            _csvText = string.Empty;
            return ParseCsvBytesAsync();
        }

        _csvTextEditDisabled = false;
        _csvText = Encoding.UTF8.GetString(file.Content);
        return ParseCsvTextAsync();
    }

    private Task ParseCsvTextAsync()
    {
        _csvError = string.Empty;
        _csvTable = null;
        _csvBytes = Encoding.UTF8.GetBytes(_csvText ?? string.Empty);
        _csvTextEditDisabled = false;
        return ApplyCsvParse(_csvBytes);
    }

    private Task ParseCsvBytesAsync()
    {
        _csvError = string.Empty;
        _csvTable = null;
        if (_csvBytes == null || _csvBytes.Length == 0) {
            _csvError = "No uploaded CSV bytes to parse.";
            return Task.CompletedTask;
        }

        return ApplyCsvParse(_csvBytes);
    }

    private Task ApplyCsvParse(byte[] bytes)
    {
        try {
            var result = CsvService.ParseBytesAsDataTable(bytes, _csvHasHeader);
            if (result.IsSuccess && result.Data != null)
                _csvTable = result.Data;
            else
                _csvError = FormatResultErrors(result);
        }
        catch (Exception ex) {
            _csvError = ex.Message;
        }

        return Task.CompletedTask;
    }

    private async Task DownloadCsvTableAsync()
    {
        if (_csvTable == null)
            return;

        var bytes = CsvService.ExportToCsvBytesFromDataTable(_csvTable);
        await Js.DownloadFile(bytes, "export.csv", FileTypeInfo.Csv.MimeType).ConfigureAwait(false);
    }

    private async Task DownloadCsvHtmlAsync()
    {
        if (_csvBytes == null)
            return;

        var html = CsvService.ExportToHtmlTable(_csvBytes, _csvHasHeader);
        var bytes = Encoding.UTF8.GetBytes(html);
        await Js.DownloadFile(bytes, "csv-preview.html", FileTypeInfo.Html.MimeType).ConfigureAwait(false);
    }

    private async Task OnXlsxUploadReadyAsync(LocalBrowserFile file)
    {
        _xlsxError = string.Empty;
        _xlsxTable = null;
        _xlsxUploadName = file.FileName;
        _xlsxBytes = file.Content;
        _xlsxByteLength = file.Content.Length;
        try {
            var result = await XlsxService.ParseXlsxBytesAsDataTableAsync(file.Content, _xlsxUseHeaderRow).ConfigureAwait(false);
            if (result.IsSuccess && result.Data != null)
                _xlsxTable = result.Data;
            else
                _xlsxError = FormatResultErrors(result);
        }
        catch (Exception ex) {
            _xlsxError = ex.Message;
        }
    }

    private async Task DownloadXlsxReexportAsync()
    {
        if (_xlsxTable == null)
            return;

        var bytes = XlsxService.Writer.ExportToXlsxBytesFromDataTable(_xlsxTable);
        await Js.DownloadFile(bytes, "export.xlsx", FileTypeInfo.Xlsx.MimeType).ConfigureAwait(false);
    }

    private async Task DownloadXlsxAsCsvAsync()
    {
        if (_xlsxBytes == null)
            return;

        var bytes = await XlsxService.ConvertXlsxToCsvBytesAsync(_xlsxBytes).ConfigureAwait(false);
        await Js.DownloadFile(bytes, Path.ChangeExtension(_xlsxUploadName, ".csv"), FileTypeInfo.Csv.MimeType).ConfigureAwait(false);
    }

    private async Task DownloadXlsxHtmlAsync()
    {
        if (_xlsxBytes == null)
            return;

        var html = await XlsxService.ExportToHtmlTableAsync(_xlsxBytes, _xlsxUseHeaderRow).ConfigureAwait(false);
        var bytes = Encoding.UTF8.GetBytes(html);
        await Js.DownloadFile(bytes, "xlsx-preview.html", FileTypeInfo.Html.MimeType).ConfigureAwait(false);
    }

    private Task OnMergeAReadyAsync(LocalBrowserFile file)
    {
        _mergeNameA = file.FileName;
        _mergeBytesA = file.Content;
        _mergeError = string.Empty;
        _mergedTable = null;
        _mergedCombinedBytes = null;
        return Task.CompletedTask;
    }

    private Task OnMergeBReadyAsync(LocalBrowserFile file)
    {
        _mergeNameB = file.FileName;
        _mergeBytesB = file.Content;
        _mergeError = string.Empty;
        _mergedTable = null;
        _mergedCombinedBytes = null;
        return Task.CompletedTask;
    }

    private Task OnMultiCombineFileReadyAsync(LocalBrowserFile file)
    {
        _multiCombineFiles.Add(file);
        _mergeError = string.Empty;
        _mergedTable = null;
        _mergedCombinedBytes = null;
        return Task.CompletedTask;
    }

    private Task OnMultiCombineFileRemovedAsync(LocalBrowserFile file)
    {
        _multiCombineFiles.Remove(file);
        _mergedTable = null;
        _mergedCombinedBytes = null;
        return Task.CompletedTask;
    }

    private Task OnSplitUploadReadyAsync(LocalBrowserFile file)
    {
        _splitBytes = file.Content;
        _splitFileName = file.FileName;
        _splitIsXlsx = IsXlsx(file.FileName);
        _splitError = string.Empty;
        _splitPartCount = 0;
        return Task.CompletedTask;
    }

    private async Task MergeAsync()
    {
        _mergeError = string.Empty;
        _mergedTable = null;
        _mergedCombinedBytes = null;
        if (_mergeBytesA == null || _mergeBytesB == null) {
            _mergeError = "Upload both sources.";
            return;
        }

        try {
            if (IsCsv(_mergeNameA) && IsCsv(_mergeNameB) && _mergeSkipFirstRowB) {
                _mergedCombinedBytes = await CsvService.CombineCsvBytesAsync([_mergeBytesA, _mergeBytesB]).ConfigureAwait(false);
                _mergedCombinedIsXlsx = false;
                var parsed = await CsvService.ParseBytesAsDataTableAsync(_mergedCombinedBytes, _mergeHeaderA).ConfigureAwait(false);
                if (!parsed.IsSuccess || parsed.Data == null) {
                    _mergeError = FormatResultErrors(parsed);
                    return;
                }

                _mergedTable = parsed.Data;
            }
            else if (IsXlsx(_mergeNameA) && IsXlsx(_mergeNameB)) {
                _mergedCombinedBytes = await XlsxService.MergeXlsxBytesAsync([_mergeBytesA, _mergeBytesB], _xlsxMergeMode).ConfigureAwait(false);
                _mergedCombinedIsXlsx = true;
                _mergedTable = await ParseMergedXlsxPreviewAsync(_mergedCombinedBytes, _xlsxMergeMode).ConfigureAwait(false);
            }
            else {
                var a = await ParseToDataTableAsync(_mergeNameA, _mergeBytesA, _mergeHeaderA).ConfigureAwait(false);
                var b = await ParseToDataTableAsync(_mergeNameB, _mergeBytesB, _mergeHeaderB).ConfigureAwait(false);
                if (!a.IsSuccess || a.Data == null) {
                    _mergeError = "Source A: " + FormatResultErrors(a);
                    return;
                }

                if (!b.IsSuccess || b.Data == null) {
                    _mergeError = "Source B: " + FormatResultErrors(b);
                    return;
                }

                _mergedTable = TabularDataMergeHelper.AppendRows(a.Data, b.Data, _mergeSkipFirstRowB);
            }

            Snackbar.Add($"Merged {_mergedTable.Rows.Count} row(s).", Severity.Success);
        }
        catch (Exception ex) {
            _mergeError = ex.Message;
        }
    }

    private async Task CombineMultipleAsync()
    {
        _mergeError = string.Empty;
        _mergedTable = null;
        _mergedCombinedBytes = null;
        if (_multiCombineFiles.Count < 2) {
            _mergeError = "Upload at least two files.";
            return;
        }

        try {
            if (_multiCombineAllCsv) {
                _mergedCombinedBytes = await CsvService.CombineCsvBytesAsync(_multiCombineFiles.Select(f => f.Content)).ConfigureAwait(false);
                _mergedCombinedIsXlsx = false;
                var parsed = await CsvService.ParseBytesAsDataTableAsync(_mergedCombinedBytes, true).ConfigureAwait(false);
                if (!parsed.IsSuccess || parsed.Data == null) {
                    _mergeError = FormatResultErrors(parsed);
                    return;
                }

                _mergedTable = parsed.Data;
            }
            else if (_multiCombineAllXlsx) {
                _mergedCombinedBytes = await XlsxService.MergeXlsxBytesAsync(_multiCombineFiles.Select(f => f.Content), _xlsxMergeMode).ConfigureAwait(false);
                _mergedCombinedIsXlsx = true;
                _mergedTable = await ParseMergedXlsxPreviewAsync(_mergedCombinedBytes, _xlsxMergeMode).ConfigureAwait(false);
            }
            else {
                _mergeError = "All files must be the same type (.csv or .xlsx).";
                return;
            }

            Snackbar.Add($"Combined {_multiCombineFiles.Count} file(s); preview shows {_mergedTable.Rows.Count} row(s).", Severity.Success);
        }
        catch (Exception ex) {
            _mergeError = ex.Message;
        }
    }

    private async Task SplitAndDownloadZipAsync()
    {
        _splitError = string.Empty;
        _splitPartCount = 0;
        if (_splitBytes == null) {
            _splitError = "Upload a file first.";
            return;
        }

        if (_splitRowsPerFile < 1 && (!_splitIsXlsx || _splitXlsxMode == "rows")) {
            _splitError = "Rows per part must be at least 1.";
            return;
        }

        try {
            Dictionary<string, byte[]> entries;
            if (_splitIsXlsx) {
                if (_splitXlsxMode == "sheet") {
                    var parts = XlsxService.SplitXlsxBytesBySheet(_splitBytes);
                    entries = parts.ToDictionary(kv => $"{SanitizeZipEntryName(kv.Key)}.xlsx", kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                }
                else {
                    var sheetName = string.IsNullOrWhiteSpace(_splitSheetName) ? null : _splitSheetName.Trim();
                    var parts = await XlsxService.SplitXlsxBytesByRowsAsync(_splitBytes, _splitRowsPerFile, sheetName).ConfigureAwait(false);
                    entries = parts.Select((part, index) => ($"part_{index + 1}.xlsx", part)).ToDictionary(x => x.Item1, x => x.part);
                }
            }
            else {
                var parts = await CsvService.SplitCsvBytesAsync(_splitBytes, _splitRowsPerFile).ConfigureAwait(false);
                entries = parts.Select((part, index) => ($"part_{index + 1}.csv", part)).ToDictionary(x => x.Item1, x => x.part);
            }

            if (entries.Count == 0) {
                _splitError = "Nothing to split (file may be empty).";
                return;
            }

            var zipBytes = CreateZipArchive(entries);
            var baseName = Path.GetFileNameWithoutExtension(_splitFileName);
            await Js.DownloadFile(zipBytes, $"{baseName}-split.zip", FileTypeInfo.Zip.MimeType).ConfigureAwait(false);
            _splitPartCount = entries.Count;
            Snackbar.Add($"Downloaded zip with {_splitPartCount} part(s).", Severity.Success);
        }
        catch (Exception ex) {
            _splitError = ex.Message;
        }
    }

    private async Task<Lyo.DataTable.Models.DataTable> ParseMergedXlsxPreviewAsync(byte[] mergedBytes, XlsxMergeMode mode)
    {
        if (mode == XlsxMergeMode.ConcatenateRows) {
            var result = await XlsxService.ParseXlsxBytesAsDataTableAsync(mergedBytes, "Merged").ConfigureAwait(false);
            return result.ValueOrThrow();
        }

        var sheets = XlsxService.ParseXlsxBytesAsAllSheets(mergedBytes);
        return sheets.Values.First();
    }

    private async Task<Result<Lyo.DataTable.Models.DataTable>> ParseToDataTableAsync(string fileName, byte[] bytes, bool useHeaderRow)
    {
        if (IsCsv(fileName))
            return await CsvService.ParseBytesAsDataTableAsync(bytes, useHeaderRow).ConfigureAwait(false);

        if (IsXlsx(fileName))
            return await XlsxService.ParseXlsxBytesAsDataTableAsync(bytes, useHeaderRow).ConfigureAwait(false);

        return Result<Lyo.DataTable.Models.DataTable>.Failure($"Unsupported type: {fileName}. Use .csv or .xlsx.", "UNSUPPORTED");
    }

    private static bool IsCsv(string name) => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    private static bool IsXlsx(string name) => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    private async Task DownloadMergedCsvAsync()
    {
        if (_mergedCombinedBytes != null && !_mergedCombinedIsXlsx) {
            await Js.DownloadFile(_mergedCombinedBytes, "merged.csv", FileTypeInfo.Csv.MimeType).ConfigureAwait(false);
            return;
        }

        if (_mergedTable == null)
            return;

        var bytes = CsvService.ExportToCsvBytesFromDataTable(_mergedTable);
        await Js.DownloadFile(bytes, "merged.csv", FileTypeInfo.Csv.MimeType).ConfigureAwait(false);
    }

    private async Task DownloadMergedXlsxAsync()
    {
        if (_mergedCombinedBytes != null && _mergedCombinedIsXlsx) {
            await Js.DownloadFile(_mergedCombinedBytes, "merged.xlsx", FileTypeInfo.Xlsx.MimeType).ConfigureAwait(false);
            return;
        }

        if (_mergedTable == null)
            return;

        var bytes = XlsxService.Writer.ExportToXlsxBytesFromDataTable(_mergedTable);
        await Js.DownloadFile(bytes, "merged.xlsx", FileTypeInfo.Xlsx.MimeType).ConfigureAwait(false);
    }

    private Task OnConvertXlsxReadyAsync(LocalBrowserFile file)
    {
        _convertXlsxName = file.FileName;
        _convertXlsxBytes = file.Content;
        return Task.CompletedTask;
    }

    private async Task DownloadConvertedCsvAsync()
    {
        if (_convertXlsxBytes == null)
            return;

        var bytes = await XlsxService.ConvertXlsxToCsvBytesAsync(_convertXlsxBytes).ConfigureAwait(false);
        await Js.DownloadFile(bytes, Path.ChangeExtension(_convertXlsxName, ".csv"), FileTypeInfo.Csv.MimeType).ConfigureAwait(false);
    }

    private async Task OnJsonPreviewUploadAsync(LocalBrowserFile file)
    {
        _dictJsonRoot = null;
        _dictJsonError = null;
        _dictJsonBytes = null;
        _dictJsonFileName = Path.ChangeExtension(file.FileName, ".json");
        try {
            object? payload = null;
            if (IsCsv(file.FileName))
                payload = await CsvService.ParseBytesAsDictionaryAsync(file.Content).ConfigureAwait(false);
            else if (IsXlsx(file.FileName))
                payload = XlsxService.ParseXlsxBytesAsDictionary(file.Content);
            else {
                _dictJsonError = "Use a .csv or .xlsx file.";
                return;
            }

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _dictJsonRoot = JsonNode.Parse(json);
            _dictJsonBytes = Encoding.UTF8.GetBytes(json);
            _dictJsonViewerKey++;
        }
        catch (Exception ex) {
            _dictJsonError = ex.Message;
            _dictJsonRoot = null;
        }
    }

    private async Task DownloadDictJsonAsync()
    {
        if (_dictJsonBytes == null)
            return;

        await Js.DownloadFile(_dictJsonBytes, string.IsNullOrEmpty(_dictJsonFileName) ? "grid.json" : _dictJsonFileName, FileTypeInfo.Json.MimeType).ConfigureAwait(false);
    }

    private static string FormatResultErrors<T>(Result<T> result)
    {
        if (result.Errors != null && result.Errors.Count > 0)
            return string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}"));

        return "Operation failed.";
    }

    private static byte[] CreateZipArchive(IReadOnlyDictionary<string, byte[]> entries)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true)) {
            foreach (var (fileName, content) in entries) {
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }
        }

        return zipStream.ToArray();
    }

    private static string SanitizeZipEntryName(string value) => PathHelpers.SanitizeFileName(value) ?? "part";
}
