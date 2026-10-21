using Lyo.Exceptions.Models;

namespace Lyo.Job.Scheduler;

/// <summary>Settings for <see cref="JobWorkflowScheduler" />.</summary>
public sealed class JobWorkflowSchedulerOptions
{
    public const string SectionName = "JobWorkflowScheduler";

    /// <summary>Base URL of the Job API, e.g. <c>http://localhost:5000/api</c>.</summary>
    public required string ApiBaseUrl { get; set; }

    /// <summary>How often workflow-owned schedules are checked for a due slot.</summary>
    public int ScheduleCheckIntervalSeconds { get; set; } = 30;

    /// <summary>Zone used when a schedule does not name one of its own. Null means UTC.</summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// How far back the due-slot search anchors when a schedule has never run. Too small and a schedule created mid-interval waits a full cycle before its first run; too large
    /// and a long-dormant schedule fires for a slot nobody wanted. Default 1440 (24h), matching <c>JobSchedulerOptions</c>.
    /// </summary>
    public int MisfireLookbackMinutes { get; set; } = 1440;

    /// <summary>Value stamped on the runs this scheduler starts.</summary>
    public string CreatedBy { get; set; } = "WorkflowScheduler";

    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            errors.Add($"{nameof(ApiBaseUrl)} is required.");

        if (ScheduleCheckIntervalSeconds <= 0)
            errors.Add($"{nameof(ScheduleCheckIntervalSeconds)} must be greater than zero.");

        if (MisfireLookbackMinutes < 0)
            errors.Add($"{nameof(MisfireLookbackMinutes)} must be 0 or greater.");

        return errors;
    }

    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(JobWorkflowSchedulerOptions)}: {string.Join(" ", errors)}");
    }
}