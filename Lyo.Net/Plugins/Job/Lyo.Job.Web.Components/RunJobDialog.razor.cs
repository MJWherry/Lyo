using Lyo.Parameters;
using Lyo.Web.Components.ParamTable;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Web.Components;

public partial class RunJobDialog
{
    static RunJobDialog() => FormatterLyoType.EnsureRegistered();

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IEnumerable<JobDefinitionRes>? JobDefinitions { get; set; }

    [Parameter]
    public JobDefinitionRes? SelectedJobDefinition { get; set; }

    [Parameter]
    [EditorRequired]
    public string RunRoute { get; set; } = "";

    [Inject]
    IApiClient ApiClient { get; set; } = null!;

    /// <summary>Resolved with <c>GetService</c> so a host without a formatter still opens the dialog; expression defaults are then resolved on the server on submit.</summary>
    [Inject]
    IServiceProvider Services { get; set; } = null!;

    private JobDefinitionRes? _selectedDef;
    private string _createdBy = "gateway";
    private bool _allowTriggers = true;
    private bool _submitting;
    private List<LyoParameterEntry> _parameters = [];

    // Rebuilt only when the definition changes; the formatter editor rebuilds its token catalog whenever it is given a new context instance.
    private object? _formatterContext;

    protected override void OnInitialized()
    {
        if (SelectedJobDefinition != null)
            OnDefinitionChanged(SelectedJobDefinition);
    }

    private void OnDefinitionChanged(JobDefinitionRes? def)
    {
        _selectedDef = def;
        _parameters = LyoParameterEntry.From(def?.JobParameters, p => p.Order, Services.GetService<LyoTemplateResolver>());
        _formatterContext = JobFormatterContext.Build(def);
    }

    private bool IsValid() => _selectedDef != null && !string.IsNullOrWhiteSpace(_createdBy) && LyoParameterEntry.RequiredSatisfied(_parameters);

    private async Task Submit()
    {
        if (_selectedDef == null)
            return;

        _submitting = true;
        try {
            var runParams = _parameters.Where(p => p.ShouldSubmit)
                .Select(p => new JobRunParameterReq { Key = p.Definition.Key, Type = p.Definition.Type, Value = p.SubmitValue, Description = p.Definition.Description })
                .ToList();

            var req = new JobRunReq {
                JobDefinitionId = _selectedDef.Id,
                CreatedBy = _createdBy,
                AllowTriggers = _allowTriggers,
                JobRunParameters = runParams
            };

            var result = await ApiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(RunRoute + "/Create", req);
            Snackbar.Add($"Job run {result?.Data?.Id.Truncated() ?? "?"} created", Severity.Success);
            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed: {ex.Message}", Severity.Error);
        }
        finally {
            _submitting = false;
        }
    }
}
