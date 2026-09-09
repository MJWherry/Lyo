namespace Lyo.Job.Web.Components;

/// <summary>QueryConcrete includes required when <see cref="JobDefinitionView" /> is opened.</summary>
internal static class JobDefinitionEditorQuery
{
    /// <summary>Parameters, schedules (including schedule params and blackout windows), and triggers.</summary>
    public static readonly string[] Includes = [
        "JobParameters",
        "JobSchedules.JobScheduleParameters",
        "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
        "JobTriggerTriggersJobDefinitions"
    ];
}
