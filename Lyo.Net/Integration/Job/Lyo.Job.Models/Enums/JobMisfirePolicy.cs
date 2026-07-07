namespace Lyo.Job.Models.Enums;

/// <summary>How schedule slots that were missed (e.g. while no scheduler was running) are handled.</summary>
public enum JobMisfirePolicy
{
    /// <summary>Missed slots are skipped; the schedule resumes at its next regular slot.</summary>
    Skip = 0,

    /// <summary>A single catch-up run is created for the most recent missed slot.</summary>
    RunOnce = 1
}
