using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.ParamTable;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class RunReportDialog
{
    static RunReportDialog() => FormatterLyoType.EnsureRegistered();

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IEnumerable<ReportDefinitionRes>? Definitions { get; set; }

    [Parameter]
    public ReportDefinitionRes? SelectedDefinition { get; set; }

    [Parameter]
    [EditorRequired]
    public string GenerateRoute { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Resolved with <c>GetService</c> so a host without a formatter still opens the dialog; expression defaults are then resolved on the server on submit.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private ReportDefinitionRes? _selected;
    private ReportFormat? _formatOverride;
    private string? _fileName;
    private bool _busy;
    private List<LyoParameterEntry> _parameters = [];

    // Rebuilt on definition and value changes only; the formatter editor rebuilds its token catalog whenever it is given a new context instance.
    private object? _formatterContext;

    protected override void OnParametersSet()
    {
        if (_selected is null && (SelectedDefinition is not null || Definitions is not null))
            OnDefinitionChanged(SelectedDefinition ?? Definitions?.FirstOrDefault(d => d.IsActive));
    }

    private void OnDefinitionChanged(ReportDefinitionRes? def)
    {
        _selected = def;
        _parameters = LyoParameterEntry.From(def?.Parameters, resolver: Services.GetService<LyoTemplateResolver>());
        RebuildFormatterContext();
    }

    private void OnValueChanged()
    {
        RebuildFormatterContext();
        StateHasChanged();
    }

    private void RebuildFormatterContext() => _formatterContext = ReportFormatterContext.Build(_selected, LyoParameterEntry.SiblingMap(_parameters));

    private bool IsValid() => _selected is not null && LyoParameterEntry.RequiredSatisfied(_parameters);

    private async Task Submit()
    {
        if (_selected is null)
            return;

        _busy = true;
        try {
            var parameters = _parameters.Where(p => p.ShouldSubmit)
                .Select(p => new ReportGenerationParameterReq { Key = p.Definition.Key, Type = p.Definition.Type, Value = p.SubmitValue, Description = p.Definition.Description })
                .ToList();

            var req = new GenerateReportReq {
                ReportDefinitionId = _selected.Id,
                Format = _formatOverride,
                FileName = string.IsNullOrWhiteSpace(_fileName) ? null : _fileName,
                Parameters = parameters
            };

            var result = await ApiClient.PostAsAsync<GenerateReportReq, ReportGenerationRes>(GenerateRoute, req);
            if (result.Status == ReportGenerationStatus.Succeeded)
                Snackbar.Add($"Report generated ({result.Format}).", Severity.Success);
            else
                Snackbar.Add($"Generation finished with status {result.Status}.", Severity.Warning);

            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (Exception ex) {
            Snackbar.Add($"Generate failed: {ex.Message}", Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
