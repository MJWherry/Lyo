namespace Lyo.Job.Web.Components;

/// <summary>
/// One workflow run: the authored graph with each step's outcome overlaid, plus a per-step drill-down into the job run it produced. Seeing the graph and the step table
/// together is what makes a gated-off branch legible — the table alone cannot show that a Skipped step was downstream of one whose condition did not match.
/// </summary>
public partial class JobWorkflowRunDetailView
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public JobWorkflowRunRes? Run { get; set; }

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    private IReadOnlyList<JobWorkflowStepRes> Definitions => Run?.JobWorkflow?.Steps ?? [];

    private IReadOnlyList<JobWorkflowRunStepRes> OrderedRunSteps
        => [.. (Run?.RunSteps ?? []).OrderBy(rs => Definitions.FirstOrDefault(d => d.Id == rs.JobWorkflowStepId)?.StepOrder ?? int.MaxValue)];

    /// <summary>The run's step definitions rebuilt as drafts so the graph view lays out the executed run exactly as the builder laid out the authored workflow.</summary>
    private IReadOnlyList<JobWorkflowStepDraft> GraphSteps
        => [
            .. Definitions.Select(d => new JobWorkflowStepDraft {
                Id = d.Id,
                StepName = d.StepName,
                JobDefinitionId = d.JobDefinitionId,
                DependsOn = [.. DependencyIds(d)],
                FailurePolicy = d.FailurePolicy,
                Enabled = d.Enabled,
                Condition = JobWorkflowConditionEvaluator.TryParse(d.ConditionJson)
            })
        ];

    private IReadOnlyDictionary<Guid, JobWorkflowStepState> StepStates => (Run?.RunSteps ?? []).GroupBy(rs => rs.JobWorkflowStepId).ToDictionary(g => g.Key, g => g.First().State);

    private static IEnumerable<Guid> DependencyIds(JobWorkflowStepRes step)
    {
        JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var ids);
        return ids;
    }

    private string StepName(JobWorkflowRunStepRes runStep) => Definitions.FirstOrDefault(d => d.Id == runStep.JobWorkflowStepId)?.StepName ?? "—";

    private async Task ViewJobRunAsync(JobWorkflowRunStepRes runStep)
    {
        if (runStep.JobRunId is not { } runId)
            return;

        var runRoute = $"{BaseRoute.TrimEnd('/')}/Run";
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
            $"{runRoute}/QueryConcrete", new() { Keys = [[runId]], Amount = 1, Include = ["JobDefinition", "JobRunParameters", "JobRunResults", "JobRunLogs"] });

        if (res?.Items?.FirstOrDefault() is not { } jobRun)
            return;

        var parameters = new DialogParameters<JobRunDetailView> { { d => d.JobRun, jobRun }, { d => d.RunRoute, runRoute } };
        await DialogService.ShowAsync<JobRunDetailView>("Job run", parameters, LyoDialogPresets.Large);
    }
}