using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Authentication.Web.Components.Pages;

public partial class DebugPage
{
    /// <summary>Optional replacement for the root element id consumed by the element-id scheme.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    private AuthSessionSnapshot? _snapshot;

    private bool _loading = true;

    private bool _busy;

    private string _headerJson = string.Empty;

    private string _payloadJson = string.Empty;

    private string _signatureSegment = string.Empty;

    private bool IsExpired => _snapshot is not null && _snapshot.AccessTokenExpiresAt <= DateTime.UtcNow;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        try {
            _snapshot = await SessionAccessor.GetCurrentAsync();
            DecodeJwt();
        }
        finally {
            _loading = false;
        }
    }

    private async Task RefreshTokenAsync()
    {
        _busy = true;
        try {
            if (await SessionAccessor.RefreshAsync()) {
                Snackbar.Add("Token refreshed.", Severity.Success);
                _snapshot = await SessionAccessor.GetCurrentAsync();
                DecodeJwt();
            }
            else {
                Snackbar.Add("Refresh failed.", Severity.Warning);
            }
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task CopyAsync(string value)
    {
        try {
            await JsInterop.SendToClipboard(value);
            Snackbar.Add("Copied.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Copy failed: {ex.Message}", Severity.Error);
        }
    }

    private void DecodeJwt()
    {
        if (_snapshot is null || string.IsNullOrWhiteSpace(_snapshot.AccessToken)) {
            _headerJson = _payloadJson = _signatureSegment = string.Empty;
            return;
        }

        var parts = _snapshot.AccessToken.Split('.');
        _headerJson = parts.Length > 0 ? PrettyJsonOrRaw(parts[0]) : string.Empty;
        _payloadJson = parts.Length > 1 ? PrettyJsonOrRaw(parts[1]) : string.Empty;
        _signatureSegment = parts.Length > 2 ? parts[2] : string.Empty;
    }

    private static string PrettyJsonOrRaw(string segment)
    {
        try {
            var bytes = Base64Url.Decode(segment);
            using var doc = JsonDocument.Parse(bytes);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception) {
            return Encoding.UTF8.GetString(SafeDecode(segment));
        }
    }

    private static byte[] SafeDecode(string segment)
    {
        try {
            return Base64Url.Decode(segment);
        }
        catch (FormatException) {
            return Array.Empty<byte>();
        }
    }

    private static string DescribeRemaining(DateTime expiresAt)
    {
        var remaining = expiresAt - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return "expired";

        if (remaining.TotalMinutes < 1)
            return $"{(int)remaining.TotalSeconds}s remaining";

        if (remaining.TotalHours < 1)
            return $"{(int)remaining.TotalMinutes}m remaining";

        return $"{(int)remaining.TotalHours}h remaining";
    }
}
