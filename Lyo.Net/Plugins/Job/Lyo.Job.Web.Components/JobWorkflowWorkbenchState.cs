namespace Lyo.Job.Web.Components;

/// <summary>
/// The in-progress workflow the workbench persists through <c>ClientStore</c>. Authoring a DAG takes many small edits, so a page reload that discarded them would make the
/// editor unusable for anything non-trivial. Only the draft is stored — never run results, which belong to the server.
/// </summary>
public sealed class JobWorkflowWorkbenchState
{
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobWorkflowStepDraft> Steps { get; set; } = [];

    /// <summary>
    /// Run inputs the workflow is started with, keyed by name. They serve two purposes: the keys are what the step editor offers as <c>Input.{key}</c> paths, and the
    /// values are what <b>Run now</b> sends, so a condition that branches on an input can actually be exercised from here.
    /// </summary>
    public Dictionary<string, string> Inputs { get; set; } = [];

    /// <summary>Draft id of the step the editor pane is showing. Persisted so a reload returns to that step, and a rename does not lose it.</summary>
    public Guid SelectedStepId { get; set; }

    /// <summary>Name of the selected step in drafts saved before selection was an id. <see cref="JobWorkflowDraftLinks.Repair"/> resolves it once.</summary>
    public string? SelectedStepName { get; set; }

    /// <summary>Workflow that the run panel last started, so reopening the workbench reconnects to that run history.</summary>
    public Guid? LastStartedWorkflowId { get; set; }
}
