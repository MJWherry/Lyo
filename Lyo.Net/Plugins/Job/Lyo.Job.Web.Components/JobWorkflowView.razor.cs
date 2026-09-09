namespace Lyo.Job.Web.Components;

public partial class JobWorkflowView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    private string _workflowRoute => $"{BaseRoute.TrimEnd('/')}/Workflow";

    private bool _loading;
    private Guid? _selectedWorkflowId;
    private JobWorkflowRes? _selectedWorkflow;
    private IReadOnlyList<JobWorkflowRes> _workflows = [];

    protected override async Task OnInitializedAsync() => await LoadWorkflowListAsync();

    private async Task LoadWorkflowListAsync()
    {
        _loading = true;
        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobWorkflowRes>>($"{_workflowRoute}/QueryConcrete", new() { Amount = 200, Include = ["JobWorkflowSteps", "JobWorkflowSteps.JobDefinition"] });
            _workflows = res?.Items ?? [];
            if (_selectedWorkflowId is null && _workflows.Count > 0)
                _selectedWorkflowId = _workflows[0].Id;
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to load workflows: {ex.Message}", Severity.Error);
        }
        finally {
            _loading = false;
        }

        await LoadWorkflowAsync();
    }

    /// <summary>Selecting a workflow has to load it: the dropdown is the only way to change which workflow is shown, and a two-way bind by itself never triggers a fetch.</summary>
    private async Task OnWorkflowSelectedAsync(Guid? workflowId)
    {
        if (_selectedWorkflowId == workflowId)
            return;

        _selectedWorkflowId = workflowId;
        await LoadWorkflowAsync();
    }

    private async Task LoadWorkflowAsync()
    {
        if (_selectedWorkflowId is null) {
            _selectedWorkflow = null;
            return;
        }

        _loading = true;
        try {
            var include = string.Join("&include=", "JobWorkflowSteps", "JobWorkflowSteps.JobDefinition");
            _selectedWorkflow = await ApiClient.GetAsAsync<JobWorkflowRes>($"{_workflowRoute}/{_selectedWorkflowId}?include={include}");
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to load workflow: {ex.Message}", Severity.Error);
        }
        finally {
            _loading = false;
        }
    }

    private static Color GetStepColor(JobWorkflowStepRes step) => step.Enabled ? Color.Primary : Color.Default;
}
