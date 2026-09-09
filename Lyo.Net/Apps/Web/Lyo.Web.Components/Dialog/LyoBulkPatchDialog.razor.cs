using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.Dialog;

/// <summary>
/// Picks one property and a typed value, then PATCHes the selected keys through <see cref="IApiClient" />. Completes the bulk-action toolbar that already ships
/// export and delete.
/// </summary>
public partial class LyoBulkPatchDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Composite keys of the selected rows, in the same shape the delete endpoint already accepts.</summary>
    [Parameter]
    public IReadOnlyList<object[]> Keys { get; set; } = [];

    /// <summary>Patchable columns. When empty, the user types a property name and the value editor default is string.</summary>
    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> Properties { get; set; } = [];

    /// <summary>PATCH route. <c>/Bulk</c> is appended when it is not already present.</summary>
    [Parameter]
    [EditorRequired]
    public string PatchRoute { get; set; } = string.Empty;

    /// <summary>API client that sends the patch.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string? _property;
    private string? _json;
    private bool _busy;
    private string? _error;

    private FilterPropertyDefinition? SelectedProperty
        => Properties.FirstOrDefault(property => string.Equals(property.PropertyName, _property, StringComparison.Ordinal));

    private string TypeName => SelectedProperty?.Type.FullName ?? LyoTypeInfo.String.FullName;

    private bool CanApply => Keys.Count > 0 && !string.IsNullOrWhiteSpace(_property) && !string.IsNullOrWhiteSpace(PatchRoute);

    private object DebugPayload => new { Keys, Property = _property, Json = _json, Route = BulkRoute };

    private string BulkRoute
    {
        get
        {
            var route = PatchRoute.TrimEnd('/');
            return route.EndsWith("/Bulk", StringComparison.OrdinalIgnoreCase) ? route : route + "/Bulk";
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(_property) && Properties.Count > 0)
            _property = Properties[0].PropertyName;
    }

    private void OnPropertyChanged(string value)
    {
        _property = value;
        _json = SelectedProperty?.Type.DefaultJson;
        _error = null;
    }

    private void OnJsonChanged(string? json) => _json = json;

    private async Task ApplyAsync()
    {
        if (!CanApply || string.IsNullOrWhiteSpace(_property))
            return;

        _busy = true;
        _error = null;
        try {
            object? value = null;
            if (!string.IsNullOrWhiteSpace(_json))
                value = JsonSerializer.Deserialize<object>(_json);

            var request = new PatchRequest(Keys) { AllowMultiple = true, Properties = { [_property] = value } };
            var result = await ApiClient.PatchAsAsync<IEnumerable<PatchRequest>, PatchBulkResult<object?>>(BulkRoute, [request]);
            if (result.FailedCount > 0)
                Snackbar.Add($"Patched {result.UpdatedCount} rows, {result.FailedCount} failed", Severity.Warning);
            else
                Snackbar.Add($"Patched {result.UpdatedCount} rows", Severity.Success);

            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (Exception ex) {
            _error = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
