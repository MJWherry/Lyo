using Lyo.Exceptions.Models;

namespace Lyo.Job.Scheduler;

/// <summary>Settings for the Job Scheduler service.</summary>
public sealed class JobSchedulerOptions
{
    /// <summary>Default configuration section.</summary>
    public const string SectionName = "JobScheduler";

    /// <summary>Job API base URL (for example https://api.example.com). Used to query definitions and create runs.</summary>
    public required string ApiBaseUrl { get; set; }

    /// <summary>
    /// Time zone used when evaluating schedule times. When null, schedule times are read as UTC. Use <see cref="TimeZoneInfo.FindSystemTimeZoneById" /> or
    /// <see cref="TimeZoneInfo.Local" /> to pick a zone.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Seconds between definition refreshes. Default 30.</summary>
    public int DefinitionRefreshIntervalSeconds { get; set; } = 30;

    /// <summary>Seconds between schedule checks. Default 10.</summary>
    public int ScheduleCheckIntervalSeconds { get; set; } = 10;

    /// <summary>Identity stamped as CreatedBy when the scheduler creates job runs.</summary>
    public string CreatedBy { get; set; } = "Scheduler";

    /// <summary>
    /// Default handling for schedule slots missed while no scheduler was running: skip them or run a single catch-up. Individual schedules can override this with their
    /// own misfire policy.
    /// </summary>
    public bool EnableMisfireCatchUp { get; set; } = true;

    /// <summary>Max age in minutes for a missed slot to still qualify for misfire catch-up. Older slots are always skipped. Default 1440 (24h).</summary>
    public int MisfireLookbackMinutes { get; set; } = 1440;

    /// <summary>Validates the options and returns the validation failures. Empty when valid.</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            errors.Add($"{nameof(ApiBaseUrl)} is required.");

        if (DefinitionRefreshIntervalSeconds <= 0)
            errors.Add($"{nameof(DefinitionRefreshIntervalSeconds)} must be greater than 0.");

        if (ScheduleCheckIntervalSeconds <= 0)
            errors.Add($"{nameof(ScheduleCheckIntervalSeconds)} must be greater than 0.");

        if (MisfireLookbackMinutes < 0)
            errors.Add($"{nameof(MisfireLookbackMinutes)} must be 0 or greater.");

        return errors;
    }

    /// <summary>Validates the options and throws <see cref="ValidationException" /> when invalid.</summary>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(JobSchedulerOptions)}: {string.Join(" ", errors)}");
    }
}