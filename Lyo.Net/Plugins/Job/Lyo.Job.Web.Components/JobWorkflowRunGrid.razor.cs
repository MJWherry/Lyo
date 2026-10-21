namespace Lyo.Job.Web.Components;

/// <summary>Workflow run history, optionally scoped to one workflow, with a drill-down into <see cref="JobWorkflowRunDetailView" />.</summary>
public partial class JobWorkflowRunGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("State"), new("JobWorkflow.Name", "Workflow")];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    /// <summary>Restricts the grid to one workflow's runs. Null shows every workflow's runs.</summary>
    [Parameter]
    public Guid? WorkflowId { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Workflow/Run";

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (WorkflowId is { } workflowId)
            q.AddWhere(new ConditionClause("JobWorkflowId", ComparisonOperatorEnum.Equals, workflowId));
    }

    private async Task ViewRun(object? item)
    {
        if (ProjectedValueHelper.GetValue(item, "Id") is not { } id)
            return;

        // The detail view needs the steps and their definitions, which the projected grid row does not carry.
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobWorkflowRunRes>>(
            $"{_route}/QueryConcrete",
            new() {
                Keys = [[id]],
                Amount = 1,
                Include = ["JobWorkflow", "JobWorkflow.JobWorkflowSteps", "JobWorkflowRunSteps", "JobWorkflowRunSteps.JobWorkflowStep", "JobWorkflowRunSteps.JobRun"]
            });

        if (res?.Items?.FirstOrDefault() is not { } run)
            return;

        var parameters = new DialogParameters<JobWorkflowRunDetailView> { { d => d.Run, run }, { d => d.ApiClient, ApiClient }, { d => d.BaseRoute, BaseRoute } };
        await DialogService.ShowAsync<JobWorkflowRunDetailView>("Workflow run", parameters, LyoDialogPresets.Large);
    }
}