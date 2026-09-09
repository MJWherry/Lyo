namespace Lyo.Job.Web.Components;

public partial class JobDefinitionView
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public JobDefinitionRes? JobDefinition { get; set; }

    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "";

    [Parameter]
    public string ScheduleRoute { get; set; } = "";

    [Parameter]
    public string TriggerRoute { get; set; } = "";

    [Inject]
    IApiClient ApiClient { get; set; } = null!;

    private LyoForm<EditModel>? _form;
    private EditModel? _edit;
    private bool _hasChanges;
    private bool _saving;

    private class EditModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public string Type { get; set; } = "";

        public string WorkerType { get; set; } = "";

        public bool Enabled { get; set; }

        public int MaxRetryCount { get; set; }

        public int RetryBackoffSeconds { get; set; }

        public int TimeoutMinutes { get; set; }

        public int MaxConcurrentRuns { get; set; }
    }

    protected override void OnParametersSet()
    {
        // Rebuild when the parent hands over a different definition. Keyed on identity rather than "is _edit null" so the dialog cannot keep editing the previous definition's
        // values, while a re-render of the same definition still leaves in-progress edits alone.
        if (JobDefinition == null || _edit?.Id == JobDefinition.Id)
            return;

        _edit = new() {
            Id = JobDefinition.Id,
            Name = JobDefinition.Name,
            Description = JobDefinition.Description,
            Type = JobDefinition.Type,
            WorkerType = JobDefinition.WorkerType,
            Enabled = JobDefinition.Enabled,
            MaxRetryCount = JobDefinition.MaxRetryCount,
            RetryBackoffSeconds = JobDefinition.RetryBackoffSeconds,
            TimeoutMinutes = JobDefinition.TimeoutMinutes,
            MaxConcurrentRuns = JobDefinition.MaxConcurrentRuns
        };
    }

    private async Task Save()
    {
        if (JobDefinition == null || _form == null)
            return;

        var patch = _form.BuildPatchRequest(JobDefinition.Id);
        if (patch == null) {
            Snackbar.Add("No changes to save", Severity.Info);
            return;
        }

        _saving = true;
        try {
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(DefinitionRoute, patch);
            Snackbar.Add("Definition saved", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally {
            _saving = false;
        }
    }
}
