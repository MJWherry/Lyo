using Lyo.Web.Components.ParamTable;

namespace Lyo.Job.Web.Components;

public partial class JobParameterView
{
    static JobParameterView() => FormatterLyoType.EnsureRegistered();

    [Parameter]
    public IReadOnlyList<JobParameterRes>? JobParameters { get; set; }

    /// <summary>Owning definition, used to build the formatter context offered to formatter-typed parameters. Editing still works if it is omitted.</summary>
    [Parameter]
    public JobDefinitionRes? JobDefinition { get; set; }

    [Parameter]
    public Guid JobDefinitionId { get; set; }

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "";

    private string ParameterRoute => $"{DefinitionRoute}/Parameter";

    private List<LyoParameterEditRow> _rows = [];
    private bool _dirty;

    /// <summary>Definition the current rows were built from, so a definition switch rebuilds them.</summary>
    private Guid? _loadedDefinitionId;

    /// <summary>The exact <see cref="JobParameters" /> instance already loaded. Compared by reference: the parent hands over a fresh list on each reload.</summary>
    private IReadOnlyList<JobParameterRes>? _loadedParameters;

    private object? _formatterContext;

    protected override async Task OnParametersSetAsync()
    {
        // Reload when the parent switches definitions or hands over a reloaded list. Unsaved edits are kept while the inputs are unchanged, but a one-shot guard also pinned the
        // editor to the first definition it ever received.
        if (_loadedDefinitionId == JobDefinitionId && ReferenceEquals(_loadedParameters, JobParameters))
            return;

        if (_dirty && _loadedDefinitionId == JobDefinitionId)
            return; // a reload arriving mid-edit for the same definition must not discard the operator's work

        _loadedDefinitionId = JobDefinitionId;
        _loadedParameters = JobParameters;
        _rows = (JobParameters ?? []).OrderBy(p => p.Order).ThenBy(p => p.Key)
            .Select(
                p => LyoParameterEditRow.FromMasked(
                    p.Id, p.Key, p.Type, p.Value, p.Description, p.Required, p.EncryptedValue, p.AllowedValues, p.Options, p.Order, defaultKind: p.DefaultKind,
                    defaultTemplate: p.DefaultTemplate))
            .ToList();

        _dirty = false;
        _formatterContext = JobFormatterContext.Build(JobDefinition, await LoadLatestRunsAsync());
    }

    /// <summary>
    /// Real run values make the template preview worth reading, but they are not required: a failed or empty load still leaves the run paths available through the
    /// sample shape, so a load failure is not worth interrupting the editor for.
    /// </summary>
    private async Task<JobDefinitionLatestRunsRes?> LoadLatestRunsAsync()
    {
        if (JobDefinitionId == default || string.IsNullOrWhiteSpace(DefinitionRoute))
            return null;

        try {
            var res = await ApiClient.PostAsAsync<List<Guid>, List<JobDefinitionLatestRunsRes>>($"{DefinitionRoute.TrimEnd('/')}/LatestRuns", [JobDefinitionId]);
            return res?.FirstOrDefault(r => r.JobDefinitionId == JobDefinitionId) ?? res?.FirstOrDefault();
        }
        catch (Exception) {
            return null;
        }
    }

    private JobParameterReq ToRequest(LyoParameterEditRow row)
        => new() {
            JobDefinitionId = JobDefinitionId,
            Key = row.Key,
            Type = row.Type,
            Value = row.ValueForSave,
            Description = row.Description,
            Required = row.Required,
            EncryptedValue = row.EncryptedValueForSave,
            AllowedValues = row.AllowedValues,
            Options = row.Options,
            Order = row.Order,
            DefaultKind = row.DefaultKind,
            DefaultTemplate = row.DefaultTemplateForSave
        };

    private async Task SaveChanges()
    {
        try {
            await LyoParameterEditRowSync.SaveAsync<JobParameterReq, JobParameterRes>(
                ApiClient, ParameterRoute, _rows, (JobParameters ?? []).Select(p => p.Id), ToRequest, p => p.Id);
            Snackbar.Add("Parameters saved", Severity.Success);
            _dirty = false;
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
    }
}
