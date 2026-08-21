using Lyo.Api.Client;
using Lyo.Api.FileStorage.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileAccessLink;

/// <summary>Creates a file download access link via <c>POST {ApiRoutePrefix}/files/{fileId}/access-links</c> and shows copyable public URLs.</summary>
public partial class FileAccessLinkDialog
{
    private DateTime? _notBeforeDate;
    private TimeSpan? _notBeforeTime;
    private DateTime? _expiresDate;
    private TimeSpan? _expiresTime;
    private DateTime? _windowStartDate;
    private TimeSpan? _windowStartTime;
    private DateTime? _windowEndDate;
    private TimeSpan? _windowEndTime;
    private int? _maxDownloads;
    private string? _tenantId;
    private bool _busy;
    private string? _error;
    private DownloadAccessLinkResponse? _response;
    private string? _downloadAbsoluteUrl;
    private string? _presignedReadAbsoluteUrl;

    [Inject]
    private IApiClient ApiClient { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    /// <summary>Stored file id to issue the link for.</summary>
    [Parameter]
    [EditorRequired]
    public Guid FileId { get; set; }

    /// <summary>Optional display name shown above the form.</summary>
    [Parameter]
    public string? FileName { get; set; }

    /// <summary>API group prefix, e.g. <c>Workbench/FileStorage</c>.</summary>
    [Parameter]
    public string ApiRoutePrefix { get; set; } = "Workbench/FileStorage";

    /// <summary>
    /// Public origin used to build copyable URLs (no trailing slash). When null, uses <see cref="IApiClient" /> <c>BaseAddress</c>.
    /// </summary>
    [Parameter]
    public string? PublicBaseUrl { get; set; }

    /// <summary>When true, shows not-before, access window, and tenant fields. When false, only expiry and max downloads.</summary>
    [Parameter]
    public bool ShowAdvanced { get; set; } = true;

    private void Close() => MudDialog.Close();

    private static DateTime? CombineUtc(DateTime? date, TimeSpan? time)
    {
        if (!date.HasValue)
            return null;

        var t = time ?? TimeSpan.Zero;
        return DateTime.SpecifyKind(date.Value.Date.Add(t), DateTimeKind.Utc);
    }

    private string? ToAbsoluteUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var trimmed = relativePath.Trim().TrimStart('/');
        if (!string.IsNullOrWhiteSpace(PublicBaseUrl))
            return $"{PublicBaseUrl.Trim().TrimEnd('/')}/{trimmed}";

        var baseUri = ApiClient.GetClient().BaseAddress;
        return baseUri == null ? null : new Uri(baseUri, trimmed).AbsoluteUri;
    }

    private async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        try {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text).ConfigureAwait(true);
            Snackbar.Add("Copied to clipboard.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Copy failed: {ex.Message}", Severity.Warning);
        }
    }

    private async Task OpenTabAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try {
            await JsRuntime.InvokeVoidAsync("open", url, "_blank").ConfigureAwait(true);
        }
        catch (Exception ex) {
            Snackbar.Add($"Open failed: {ex.Message}", Severity.Warning);
        }
    }

    private async Task CreateAsync()
    {
        _error = null;
        var notBefore = ShowAdvanced ? CombineUtc(_notBeforeDate, _notBeforeTime) : null;
        var expires = CombineUtc(_expiresDate, _expiresTime);
        var winStart = ShowAdvanced ? CombineUtc(_windowStartDate, _windowStartTime) : null;
        var winEnd = ShowAdvanced ? CombineUtc(_windowEndDate, _windowEndTime) : null;
        if (notBefore.HasValue && expires.HasValue && notBefore > expires) {
            _error = "Not before must be earlier than or equal to expires at.";
            return;
        }

        if (winStart.HasValue && winEnd.HasValue && winStart > winEnd) {
            _error = "Window start must be earlier than or equal to window end.";
            return;
        }

        if (_maxDownloads is <= 0) {
            _error = "Max downloads must be greater than zero when set.";
            return;
        }

        var prefix = string.IsNullOrWhiteSpace(ApiRoutePrefix) ? "Workbench/FileStorage" : ApiRoutePrefix.Trim().TrimEnd('/');
        var uri = $"{prefix}/files/{FileId:D}/access-links";
        var tenant = ShowAdvanced && !string.IsNullOrWhiteSpace(_tenantId) ? _tenantId.Trim() : null;
        _busy = true;
        try {
            var response = await ApiClient.PostAsAsync<CreateDownloadAccessLinkRequest, DownloadAccessLinkResponse>(
                uri, new(notBefore, expires, winStart, winEnd, _maxDownloads, tenant), req => {
                    if (!string.IsNullOrWhiteSpace(tenant))
                        req.Headers.TryAddWithoutValidation("X-Tenant-Id", tenant);
                }).ConfigureAwait(true);
            if (response == null) {
                _error = "The API did not return an access link.";
                return;
            }

            _response = response;
            _downloadAbsoluteUrl = ToAbsoluteUrl(response.DownloadUrl);
            _presignedReadAbsoluteUrl = ToAbsoluteUrl(response.PresignedReadUrl);
            Snackbar.Add($"Created access link {response.LinkId:D}.", Severity.Success);
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }
}
