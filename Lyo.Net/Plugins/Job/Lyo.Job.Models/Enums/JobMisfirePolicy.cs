namespace Lyo.Job.Models.Enums;

/// <summary>How schedule slots that were missed (for example while no scheduler was running) are treated.</summary>
public enum JobMisfirePolicy
{
    /// <summary>Missed slots are skipped; the schedule resumes at the next regular slot.</summary>
    Skip = 0,

    /// <summary>One catch-up run is created for the most recent missed slot.</summary>
    RunOnce = 1
}