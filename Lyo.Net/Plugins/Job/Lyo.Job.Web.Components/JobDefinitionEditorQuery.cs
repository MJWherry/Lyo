namespace Lyo.Job.Web.Components;

/// <summary>QueryConcrete includes required when <see cref="JobDefinitionView" /> is opened.</summary>
internal static class JobDefinitionEditorQuery
{
    /// <summary>Parameters and schedules, including schedule parameters and blackout windows.</summary>
    public static readonly string[] Includes = [
        "JobParameters",
        "JobSchedules.JobScheduleParameters",
        "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows"
    ];
}
