using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Inline boolean column (an "Enabled"/"Active" chip or switch) backed by a PATCH. Holds the values the user has toggled since the last refresh so the chip flips immediately
/// instead of waiting for a full grid reload, and issues the PATCH itself.
/// </summary>
/// <remarks>
/// Create one per toggleable column and clear it whenever the grid reloads, otherwise a stale override will mask the server value:
/// <code>
/// private readonly ProjectedBoolOverrides _enabled = new("Enabled");
///
/// private async Task RefreshAsync()
/// {
///     _enabled.Clear();
///     await _dataGrid!.RefreshData();
/// }
/// </code>
/// </remarks>
/// <param name="field">Projected field name, also used as the PATCH property name.</param>
/// <param name="keyField">Projected field holding the row identifier.</param>
public sealed class ProjectedBoolOverrides(string field, string keyField = ProjectedGridKeys.IdField)
{
    private readonly Dictionary<Guid, bool> _overrides = [];

    /// <summary>Current value for a row: the pending override when the user has toggled it, otherwise the projected value.</summary>
    public bool Get(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, keyField);
        if (ProjectedValueHelper.TryGetGuid(id, out var guid) && _overrides.TryGetValue(guid, out var overridden))
            return overridden;

        return ProjectedValueHelper.GetBool(item, field);
    }

    /// <summary>Drops all pending overrides. Call this as part of every grid refresh.</summary>
    public void Clear() => _overrides.Clear();

    /// <summary>
    /// PATCHes one row and records the override on success. Rows whose identifier is missing or not a <see cref="Guid" /> are ignored. Failures are reported through
    /// <paramref name="snackbar" /> instead of thrown, since a failed toggle should not tear down the grid.
    /// </summary>
    /// <param name="apiClient">Client for the grid's API.</param>
    /// <param name="route">Entity route to PATCH, without the <c>/Bulk</c> suffix.</param>
    /// <param name="item">Projected row.</param>
    /// <param name="value">New value.</param>
    /// <param name="snackbar">Snackbar for the outcome message. Pass null to stay silent.</param>
    /// <param name="subject">Singular noun for the message, for example <c>Definition</c>.</param>
    /// <returns>True when the PATCH succeeded.</returns>
    public async Task<bool> PatchAsync(IApiClient apiClient, string route, object? item, bool value, ISnackbar? snackbar = null, string subject = "Record")
    {
        var id = ProjectedValueHelper.GetValue(item, keyField);
        if (!ProjectedValueHelper.TryGetGuid(id, out var guid))
            return false;

        try {
            var patch = new PatchRequestBuilder().WithKey(guid).SetProperty(field, value).Build();
            await apiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(route, patch);
            _overrides[guid] = value;
            snackbar?.Add($"{subject} {Describe(value)}", Severity.Success);
            return true;
        }
        catch (Exception ex) {
            snackbar?.Add($"Toggle failed: {ex.Message}", Severity.Error);
            return false;
        }
    }

    /// <summary>Flips one row from its current value. Convenience for row menus and clickable chips.</summary>
    /// <inheritdoc cref="PatchAsync" />
    public Task<bool> ToggleAsync(IApiClient apiClient, string route, object? item, ISnackbar? snackbar = null, string subject = "Record")
        => item == null ? Task.FromResult(false) : PatchAsync(apiClient, route, item, !Get(item), snackbar, subject);

    /// <summary>
    /// PATCHes every selected row through the bulk endpoint (<c>{route}/Bulk</c>). Callers should refresh the grid afterwards instead of relying on overrides, because a bulk patch
    /// can change rows that are not currently loaded.
    /// </summary>
    /// <param name="apiClient">Client for the grid's API.</param>
    /// <param name="route">Entity route, without the <c>/Bulk</c> suffix.</param>
    /// <param name="items">Selected projected rows.</param>
    /// <param name="value">New value.</param>
    /// <param name="snackbar">Snackbar for the outcome message. Pass null to stay silent.</param>
    /// <param name="subject">Singular noun for the message; the count and <c>(s)</c> are appended.</param>
    /// <returns>Number of rows included in the patch.</returns>
    public async Task<int> PatchManyAsync(IApiClient apiClient, string route, IEnumerable<object?>? items, bool value, ISnackbar? snackbar = null, string subject = "record")
    {
        var ids = (items ?? []).Select(i => ProjectedValueHelper.GetValue(i, keyField)).Select(v => ProjectedValueHelper.TryGetGuid(v, out var g) ? g : (Guid?)null).Where(g => g.HasValue).Select(g => g!.Value).ToList();
        if (ids.Count == 0)
            return 0;

        var patch = new PatchRequestBuilder().WithOneOfIdentifier(keyField, ids).SetProperty(field, value).AllowMultiple().Build();
        await apiClient.PatchAsAsync<PatchRequest, PatchBulkResult<object>>($"{route.TrimEnd('/')}/Bulk", patch);
        snackbar?.Add($"{Capitalize(Describe(value))} {ids.Count} {subject}(s)", Severity.Success);
        return ids.Count;
    }

    private static string Describe(bool value) => value ? "enabled" : "disabled";

    private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
