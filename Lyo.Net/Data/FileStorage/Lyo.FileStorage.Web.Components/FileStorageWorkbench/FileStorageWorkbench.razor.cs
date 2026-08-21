using System.Globalization;
using Lyo.Api.Client;
using Lyo.Api.FileStorage.Models;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.IO.Temp;
using Lyo.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

public partial class FileStorageWorkbench : ComponentBase
{
    [Inject]
    public IApiClient ApiClient { get; set; } = null!;

    [Inject]
    public IIOTempService TempService { get; set; } = null!;

    [Inject]
    public IJsInterop Js { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    /// <summary>API route segment for file metadata QueryProject (e.g. <c>Workbench/FileStorage/FileMetadata</c>).</summary>
    [Parameter]
    public string FileMetadataQueryRoute { get; set; } = "Workbench/FileStorage/FileMetadata";

    /// <summary>REST prefix for Workbench/FileStorage endpoints (matches <see cref="FileStorageWorkbenchOptions.ApiRoutePrefix" />).</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "Workbench/FileStorage";

    /// <summary>
    /// Relative URI for multipart stream upload (no <see cref="FileStorageApiRoutePrefix" />). Default <c>upload/file</c> matches <c>POST /upload/file</c>. Set to empty to use
    /// <c>{FileStorageApiRoutePrefix}/files/save-stream</c>.
    /// </summary>
    [Parameter]
    public string? StreamUploadRelativePath { get; set; } = "upload/file";

    [Parameter]
    public string Title { get; set; } = "File Storage Workbench";

    [Parameter]
    public string Description { get; set; } =
        "Upload files, migrate or rotate DEKs, and browse metadata through the file-storage HTTP API.";

    /// <summary>Raised after mutating file operations so the Browser and Tree tabs can refresh.</summary>
    public event Func<Task>? FilesChanged;

    /// <summary>True when <c>GET {prefix}/health</c> returned (the HTTP API is reachable).</summary>
    public bool ApiHealthy { get; private set; }

    /// <summary>True when the storage backend probe inside health succeeded.</summary>
    public bool StorageHealthy { get; private set; }

    /// <summary>Encryption key ids from <c>GET {prefix}/key-ids</c> (identifiers only, no key material).</summary>
    public IReadOnlyList<string> EncryptionKeyIds { get; private set; } = [];

    /// <summary>Human-readable API health line (base URL plus check message or error).</summary>
    public string ApiHealthDescription { get; private set; } = "Checking file storage API…";

    public void SetStatus(string message, Severity severity) => Snackbar.Add(message, severity);

    /// <summary>Notifies Browser grids that metadata or backing objects changed.</summary>
    public Task NotifyFilesChangedAsync() => FilesChanged?.Invoke() ?? Task.CompletedTask;

    /// <summary>Joins <see cref="FileStorageApiRoutePrefix" /> with an API-relative path such as <c>files/{id}/metadata</c>.</summary>
    public string FilesApi(string relativePath)
    {
        var prefix = FileStorageApiRoutePrefix.Trim().Trim('/');
        var relative = relativePath.Trim().TrimStart('/');
        return string.IsNullOrEmpty(prefix) ? relative : $"{prefix}/{relative}";
    }

    /// <summary>Resolves an API-relative path (e.g. <c>Workbench/FileStorage/files/...</c>) against the configured <see cref="IApiClient" /> base URL.</summary>
    /// <returns>Absolute URL when the client has <see cref="HttpClient.BaseAddress" /> set; otherwise <see langword="null" />.</returns>
    public string? GetApiAbsoluteUrl(string apiRelativePath)
    {
        if (string.IsNullOrWhiteSpace(apiRelativePath))
            return null;

        var baseUri = ApiClient.GetClient().BaseAddress;
        if (baseUri == null)
            return null;

        var trimmed = apiRelativePath.TrimStart('/');
        return new Uri(baseUri, trimmed).AbsoluteUri;
    }

    /// <summary>Builds the stream-upload URI used by the Files tab (<c>upload/file</c> or <c>{prefix}/files/save-stream</c> plus query string).</summary>
    public string BuildSaveStreamUri(
        string? originalFileName,
        bool compress,
        bool encrypt,
        string? keyId,
        string? pathPrefix,
        int? chunkSize,
        string? contentType = null,
        string? tenantId = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(originalFileName))
            parts.Add($"originalFileName={Uri.EscapeDataString(originalFileName)}");

        parts.Add($"compress={compress.ToString().ToLowerInvariant()}");
        parts.Add($"encrypt={encrypt.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(keyId))
            parts.Add($"keyId={Uri.EscapeDataString(keyId)}");

        if (!string.IsNullOrEmpty(pathPrefix))
            parts.Add($"pathPrefix={Uri.EscapeDataString(pathPrefix)}");

        if (chunkSize.HasValue)
            parts.Add($"chunkSize={chunkSize.Value.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrEmpty(contentType))
            parts.Add($"contentType={Uri.EscapeDataString(contentType)}");

        if (!string.IsNullOrEmpty(tenantId))
            parts.Add($"tenantId={Uri.EscapeDataString(tenantId)}");

        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var streamPath = StreamUploadRelativePath?.Trim().Trim('/');
        var basePath = string.IsNullOrEmpty(streamPath) ? FilesApi("files/save-stream") : streamPath;
        return $"{basePath}{qs}";
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshApiHealthAsync().ConfigureAwait(false);
        await RefreshEncryptionKeyIdsAsync().ConfigureAwait(false);
    }

    /// <summary>Loads encryption key identifiers from <c>GET {prefix}/key-ids</c>.</summary>
    public async Task RefreshEncryptionKeyIdsAsync()
    {
        try {
            var ids = await ApiClient.GetAsAsync<List<string>>(FilesApi("key-ids")).ConfigureAwait(false);
            EncryptionKeyIds = ids ?? [];
        }
        catch {
            EncryptionKeyIds = [];
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Calls <c>GET {prefix}/health</c> and updates the registration alert.</summary>
    public async Task RefreshApiHealthAsync()
    {
        var apiBase = ApiClient.GetClient().BaseAddress?.ToString().TrimEnd('/') ?? "the configured API";
        try {
            var health = await ApiClient.GetAsAsync<FileStorageHealthResponse>(FilesApi("health")).ConfigureAwait(false);
            ApiHealthy = true;
            StorageHealthy = health?.IsHealthy == true;
            var detail = string.IsNullOrWhiteSpace(health?.Message)
                ? (StorageHealthy ? "storage healthy" : "storage unhealthy")
                : health!.Message;
            ApiHealthDescription = StorageHealthy ? $"{apiBase}: {detail}" : $"{apiBase}: storage {detail}";
            if (!StorageHealthy)
                SetStatus($"File storage backend reported unhealthy ({apiBase}): {detail}", Severity.Warning);
        }
        catch (Exception ex) {
            ApiHealthy = false;
            StorageHealthy = false;
            ApiHealthDescription = $"{apiBase}: {ex.Message}";
            SetStatus($"File storage API unavailable ({apiBase}): {ex.Message}. Start Lyo.TestApi or fix ApiClient:BaseUrl.", Severity.Warning);
        }

        await InvokeAsync(StateHasChanged);
    }
}
