namespace Lyo.Job.Web.Components;

/// <summary>QueryConcrete includes required when opening <see cref="JobDefinitionView" />.</summary>
internal static class JobDefinitionEditorQuery
{
    /// <summary>Parameters, schedules (with schedule params and blackout windows), and triggers.</summary>
    public static readonly string[] Includes = [
        "JobParameters",
        "JobSchedules.JobScheduleParameters",
        "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
        "JobTriggerTriggersJobDefinitions"
    ];
}
