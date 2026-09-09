using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.ParamTable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class ReportParameterView
{
    static ReportParameterView() => FormatterLyoType.EnsureRegistered();

    [Parameter]
    public IReadOnlyList<ReportDefinitionParameterRes>? Parameters { get; set; }

    /// <summary>Owning definition, exposed to formatter-typed parameters as the <c>Definition</c> context key. Editing still works if it is omitted.</summary>
    [Parameter]
    public ReportDefinitionRes? Definition { get; set; }

    [Parameter]
    public Guid ReportDefinitionId { get; set; }

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "";

    private string ParameterRoute => $"{DefinitionRoute.TrimEnd('/')}/Parameter";

    private List<LyoParameterEditRow> _rows = [];
    private bool _dirty;
    private bool _loaded;

    // Rebuilt only on edits, not per render: the formatter editor rebuilds its token catalog whenever it is given a new context instance.
    private object? _formatterContext;

    protected override void OnParametersSet()
    {
        if (_loaded)
            return;

        _rows = (Parameters ?? []).Select(
                p => LyoParameterEditRow.FromMasked(
                    p.Id, p.Key, p.Type, p.Value, p.Description, p.Required, p.EncryptedValue, p.AllowedValues, p.Options, defaultKind: p.DefaultKind,
                    defaultTemplate: p.DefaultTemplate))
            .ToList();

        _dirty = false;
        _loaded = true;
        RebuildFormatterContext();
    }

    private void OnRowsChanged()
    {
        _dirty = true;
        RebuildFormatterContext();
    }

    private void RebuildFormatterContext()
        => _formatterContext = ReportFormatterContext.Build(Definition, _rows.Select(r => new KeyValuePair<string, string?>(r.Key, r.Value)));

    private ReportDefinitionParameterReq ToRequest(LyoParameterEditRow row)
        => new() {
            ReportDefinitionId = ReportDefinitionId,
            Key = row.Key,
            Type = row.Type,
            Value = row.ValueForSave,
            Description = row.Description,
            Required = row.Required,
            EncryptedValue = row.EncryptedValueForSave,
            AllowedValues = row.AllowedValues,
            Options = row.Options,
            DefaultKind = row.DefaultKind,
            DefaultTemplate = row.DefaultTemplateForSave
        };

    private async Task SaveChanges()
    {
        try {
            await LyoParameterEditRowSync.SaveAsync<ReportDefinitionParameterReq, ReportDefinitionParameterRes>(
                ApiClient, ParameterRoute, _rows, (Parameters ?? []).Select(p => p.Id), ToRequest, p => p.Id);
            Snackbar.Add("Parameters saved.", Severity.Success);
            _dirty = false;
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to save parameters: {ex.Message}", Severity.Error);
        }
    }
}
