using Lyo.Parameters;

namespace Lyo.Job.Web.Components;

public partial class JobRunDetailView
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public JobRunRes? JobRun { get; set; }

    [Parameter]
    [EditorRequired]
    public string RunRoute { get; set; } = "";

    [Inject]
    IApiClient ApiClient { get; set; } = null!;

    private string AlertText
        => JobRun?.JobDefinition?.AlertOnFailure == true ? "Alert on failure" : "No failure alert";

    private string ScheduleText
        => JobRun?.JobSchedule?.Description
           ?? (JobRun?.JobSchedule is { } schedule ? schedule.Type.ToString() : "—");

    private string WorkerLabel
        => JobRun?.WorkerProcessId is { } pid && !string.IsNullOrWhiteSpace(JobRun.WorkerMachineName)
            ? $"{JobRun.WorkerMachineName}:{pid}"
            : (JobRun?.WorkerMachineName ?? "—");

    private IReadOnlyList<ILyoParameterValue> OrderedParameters
        => (JobRun?.JobRunParameters ?? []).OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Cast<ILyoParameterValue>().ToList();

    private IReadOnlyList<JobRunResultRes> OrderedResults
        => (JobRun?.JobRunResults ?? []).OrderBy(r => r.Key, StringComparer.OrdinalIgnoreCase).ToList();

    private IReadOnlyList<JobRunLogRes> OrderedLogs
        => (JobRun?.JobRunLogs ?? []).OrderBy(l => l.Timestamp).ThenBy(l => l.Id).ToList();

    private string LogsTabText
    {
        get {
            var logs = JobRun?.JobRunLogs;
            var count = logs?.Count ?? 0;
            var errors = logs?.Count(l => l.Level is JobLogLevel.Error or JobLogLevel.Critical) ?? 0;
            return errors > 0 ? $"Logs ({count}, {errors} error{(errors == 1 ? "" : "s")})" : $"Logs ({count})";
        }
    }

    private async Task Rerun()
    {
        try {
            await ApiClient.PostAsAsync<object>($"{RunRoute}/{JobRun!.Id}/Rerun");
            Snackbar.Add("Re-run queued", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add($"Re-run failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task Cancel()
    {
        try {
            await ApiClient.PostAsAsync<object>($"{RunRoute}/{JobRun!.Id}/Cancel");
            Snackbar.Add("Cancel requested", Severity.Info);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add($"Cancel failed: {ex.Message}", Severity.Error);
        }
    }
}
