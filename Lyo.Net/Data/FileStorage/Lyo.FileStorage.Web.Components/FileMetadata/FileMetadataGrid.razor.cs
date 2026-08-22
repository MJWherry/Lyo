using System.Text;
using Lyo.Api.Client;
using Lyo.Common.Records;
using Lyo.Csv.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.FileAccessLink;
using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.Web.Components;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Lyo.Xlsx.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileMetadata;

/// <summary>
/// Query-backed file metadata grid: view, view metadata, download, zip, and access links.
/// Talks to <c>{ApiRoutePrefix}/FileMetadata</c>, <c>files/{id}/download</c>, <c>files/archive</c>, and <c>files/{id}/metadata</c>.
/// </summary>
public partial class FileMetadataGrid
{
    private static readonly string[] KeySelectFields = ["Id", "OriginalFileName", "ContentType", "OriginalFileSize", "DeletedAt", "Availability"];

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("OriginalFileName", "Name"),
        new("PathPrefix", "Path prefix"),
        new("ContentType", "Type"),
        new("Id", "ID")
    ];

    private LyoDataGridProjected? _dataGrid;

    [Inject]
    private IApiClient InjectedApiClient { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IJsInterop Js { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>Optional client override. When unset, uses the injected <see cref="IApiClient" />.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>API group prefix, e.g. <c>FileStorage</c>.</summary>
    [Parameter]
    public string ApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>QueryProject route. Defaults to <c>{ApiRoutePrefix}/FileMetadata</c>.</summary>
    [Parameter]
    public string? FileMetadataQueryRoute { get; set; }

    /// <summary>
    /// Public origin used to open download/view URLs in a new tab (no trailing slash).
    /// When null, uses <see cref="IApiClient" /> <c>BaseAddress</c>.
    /// </summary>
    [Parameter]
    public string? PublicBaseUrl { get; set; }

    /// <summary>Grid persistence key.</summary>
    [Parameter]
    public string GridKey { get; set; } = "FileMetadataGrid";

    /// <summary>When true, the access-link dialog shows not-before, window, and tenant fields.</summary>
    [Parameter]
    public bool ShowAdvancedAccessLink { get; set; } = true;

    private IApiClient Client => ApiClient ?? InjectedApiClient;

    private string Prefix => string.IsNullOrWhiteSpace(ApiRoutePrefix) ? "FileStorage" : ApiRoutePrefix.Trim().TrimEnd('/');

    private string ResolvedQueryRoute => string.IsNullOrWhiteSpace(FileMetadataQueryRoute)
        ? $"{Prefix}/FileMetadata"
        : FileMetadataQueryRoute.Trim().TrimStart('/');

    private static bool IsDeleted(object? item) => FileStorageGridRowHelper.IsRowDeleted(item);

    private static string? TryGetFileName(object? item)
    {
        var name = FileStorageGridRowHelper.GetOriginalFileNameFromRow(item);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        var stored = FileStorageGridRowHelper.GetSourceFileNameFromRow(item);
        return string.IsNullOrWhiteSpace(stored) ? null : stored;
    }

    private static string FormatSize(object? item)
    {
        var raw = ProjectedValueHelper.GetValue(item, "OriginalFileSize");
        return ProjectedValueHelper.TryGetInt64(raw, out var size)
            ? FileSizeUnitInfo.FormatBestFitAbbreviation(size, lowercaseAbbreviation: false)
            : "-";
    }

    private string FileDownloadPath(Guid fileId, bool inline)
    {
        var path = $"{Prefix}/files/{fileId:D}/download";
        return inline ? $"{path}?inline=true" : path;
    }

    private string FileArchivePath(IReadOnlyList<Guid> fileIds)
    {
        var qs = string.Join("&", fileIds.Select(id => $"id={id:D}"));
        return $"{Prefix}/files/archive?{qs}";
    }

    private string ToAbsoluteUrl(string relativePath)
    {
        var trimmed = relativePath.Trim().TrimStart('/');
        if (!string.IsNullOrWhiteSpace(PublicBaseUrl))
            return $"{PublicBaseUrl.Trim().TrimEnd('/')}/{trimmed}";

        var baseUri = Client.GetClient().BaseAddress;
        return baseUri == null ? trimmed : new Uri(baseUri, trimmed).AbsoluteUri;
    }

    private async Task ViewAsync(object? item)
    {
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(item, out var fileId) || IsDeleted(item))
            return;

        var type = FileStorageColorHelper.ResolveFileType(item);
        try {
            if (type == FileTypeInfo.Csv) {
                await ViewCsvAsync(fileId, TryGetFileName(item)).ConfigureAwait(true);
                return;
            }

            if (type == FileTypeInfo.Xlsx) {
                await ViewXlsxAsync(fileId, TryGetFileName(item)).ConfigureAwait(true);
                return;
            }

            if (type == FileTypeInfo.Html) {
                await ViewHtmlAsync(fileId, TryGetFileName(item)).ConfigureAwait(true);
                return;
            }

            await OpenFileAsync(fileId, inline: true).ConfigureAwait(true);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ViewMetadataAsync(object? item)
    {
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(item, out var fileId))
            return;

        try {
            var uri = IsDeleted(item)
                ? $"{Prefix}/files/{fileId:D}/metadata?includeDeleted=true"
                : $"{Prefix}/files/{fileId:D}/metadata";
            var result = await Client.GetAsAsync<FileStoreResult>(uri).ConfigureAwait(true);
            if (result == null) {
                Snackbar.Add($"Metadata for {fileId} was not returned.", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<FileStoreMetadataDialog> { { d => d.Metadata, result } };
            await DialogService.ShowAsync<FileStoreMetadataDialog>("File metadata", parameters, LyoDialogPresets.Medium);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private Task DownloadAsync(object? item)
    {
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(item, out var fileId) || IsDeleted(item))
            return Task.CompletedTask;

        return OpenFileAsync(fileId, inline: false);
    }

    private async Task OpenFileAsync(Guid fileId, bool inline)
    {
        var url = ToAbsoluteUrl(FileDownloadPath(fileId, inline));
        await JsRuntime.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task ViewCsvAsync(Guid fileId, string? fileName)
    {
        var csv = Services.GetService<ICsvService>();
        if (csv == null) {
            await OpenFileAsync(fileId, inline: true).ConfigureAwait(true);
            return;
        }

        var bytes = await Client.GetFileAsync(FileDownloadPath(fileId, inline: false)).ConfigureAwait(true);
        var parsed = await csv.ParseBytesAsDataTableAsync(bytes, hasHeaderRow: true).ConfigureAwait(true);
        if (!parsed.TryGetValue(out var table)) {
            Snackbar.Add(parsed.Errors is { Count: > 0 } errors ? errors[0].Message : "Could not parse CSV.", Severity.Error);
            return;
        }

        await ShowTabularPreviewAsync(fileName ?? "CSV", [new FileTabularPreviewSheet(fileName ?? "CSV", table)]).ConfigureAwait(true);
    }

    private async Task ViewXlsxAsync(Guid fileId, string? fileName)
    {
        var xlsx = Services.GetService<IXlsxService>();
        if (xlsx == null) {
            await OpenFileAsync(fileId, inline: true).ConfigureAwait(true);
            return;
        }

        var bytes = await Client.GetFileAsync(FileDownloadPath(fileId, inline: false)).ConfigureAwait(true);
        var all = await xlsx.ParseXlsxBytesAsAllSheetsAsync(bytes, useHeaderRow: true).ConfigureAwait(true);
        var sheets = all.Select(kv => new FileTabularPreviewSheet(kv.Key, kv.Value)).ToList();
        if (sheets.Count == 0) {
            Snackbar.Add("No worksheets found to preview.", Severity.Warning);
            return;
        }

        await ShowTabularPreviewAsync(fileName ?? "XLSX", sheets).ConfigureAwait(true);
    }

    private async Task ViewHtmlAsync(Guid fileId, string? fileName)
    {
        var bytes = await Client.GetFileAsync(FileDownloadPath(fileId, inline: false)).ConfigureAwait(true);
        var html = Encoding.UTF8.GetString(bytes);
        var parameters = new DialogParameters<FileHtmlPreviewDialog> { { d => d.Html, html } };
        await DialogService.ShowAsync<FileHtmlPreviewDialog>(fileName ?? "HTML", parameters, LyoDialogPresets.Large);
    }

    private async Task ShowTabularPreviewAsync(string title, IReadOnlyList<FileTabularPreviewSheet> sheets)
    {
        var parameters = new DialogParameters<FileTabularPreviewDialog> { { d => d.Sheets, sheets } };
        await DialogService.ShowAsync<FileTabularPreviewDialog>(title, parameters, LyoDialogPresets.Large);
    }

    private async Task BulkDownloadAsync()
    {
        if (_dataGrid?.SelectedItems is not { Count: > 0 } selected) {
            Snackbar.Add("Select at least one file.", Severity.Warning);
            return;
        }

        var ids = new List<Guid>();
        foreach (var row in selected) {
            if (FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var id) && !ids.Contains(id))
                ids.Add(id);
        }
        if (ids.Count == 0) {
            Snackbar.Add("No file ids in the current selection.", Severity.Warning);
            return;
        }

        try {
            var (stream, fileName, _) = await Client.GetFileStreamAsync(FileArchivePath(ids));
            await using (stream)
                await Js.DownloadFileFromStream(stream, fileName ?? "files.zip", FileTypeInfo.Zip.MimeType);

            Snackbar.Add($"Downloaded {ids.Count} file(s).", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task CreateLinkAsync(object? item)
    {
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(item, out var fileId) || IsDeleted(item))
            return;

        var parameters = new DialogParameters<FileAccessLinkDialog> {
            { d => d.FileId, fileId },
            { d => d.FileName, TryGetFileName(item) },
            { d => d.ApiRoutePrefix, Prefix },
            { d => d.PublicBaseUrl, PublicBaseUrl },
            { d => d.ShowAdvanced, ShowAdvancedAccessLink }
        };
        await DialogService.ShowAsync<FileAccessLinkDialog>("Create access link", parameters, LyoDialogPresets.Medium);
    }
}
