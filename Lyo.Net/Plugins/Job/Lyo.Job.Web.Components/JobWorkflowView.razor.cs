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
    private bool _starting;
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

    /// <summary>
    /// Starts an ad-hoc run of the selected workflow. No idempotency key is sent: an operator pressing the button twice means two runs, unlike the scheduler, whose retries
    /// must collapse onto one.
    /// </summary>
    private async Task StartWorkflowAsync()
    {
        if (_selectedWorkflowId is not { } workflowId)
            return;

        _starting = true;
        try {
            await ApiClient.PostAsAsync<JobWorkflowStartReq, JobWorkflowRunRes>($"{_workflowRoute}/{workflowId}/Start", new());
            Snackbar.Add("Workflow run started.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to start workflow: {ex.Message}", Severity.Error);
        }
        finally {
            _starting = false;
        }
    }

    private IReadOnlyList<JobWorkflowStepDraft> GraphSteps
        => (_selectedWorkflow?.Steps ?? []).Select(ToDraft).ToArray();

    private IReadOnlyList<JobDefinitionRes> GraphDefinitions
        => (_selectedWorkflow?.Steps ?? [])
            .Select(step => step.JobDefinition)
            .OfType<JobDefinitionRes>()
            .GroupBy(definition => definition.Id)
            .Select(group => group.First())
            .ToArray();

    private static JobWorkflowStepDraft ToDraft(JobWorkflowStepRes step)
    {
        JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var dependencies);
        return new JobWorkflowStepDraft {
            Id = step.Id,
            StepName = step.StepName,
            JobDefinitionId = step.JobDefinitionId,
            DependsOn = [.. dependencies],
            FailurePolicy = step.FailurePolicy,
            Enabled = step.Enabled
        };
    }
}
