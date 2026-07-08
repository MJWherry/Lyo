using Lyo.Common.Identifiers;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Postgres;

/// <summary>Shared blackout-calendar persistence helpers for definition and schedule creates.</summary>
internal static class JobBlackoutCalendarEntityHelper
{
    /// <summary>
    /// After Mapster maps a <see cref="JobDefinitionReq" />, ensures schedules inheriting the definition default share one
    /// <see cref="JobBlackoutCalendar" /> entity and schedules with explicit overrides keep their own.
    /// </summary>
    public static void ApplyDefinitionBlackoutDefaults(JobDefinitionReq src, JobDefinition dest)
    {
        if (src.CreateSchedules.Count == 0)
            return;

        var destSchedules = dest.JobSchedules.ToList();
        JobBlackoutCalendar? sharedCalendar = null;

        for (var i = 0; i < src.CreateSchedules.Count && i < destSchedules.Count; i++) {
            var srcSchedule = src.CreateSchedules[i];
            var destSchedule = destSchedules[i];

            if (HasScheduleBlackoutOverride(src, srcSchedule))
                continue;

            if (src.JobBlackoutCalendarId.HasValue) {
                destSchedule.JobBlackoutCalendarId = src.JobBlackoutCalendarId;
                destSchedule.JobBlackoutCalendar = null;
                continue;
            }

            if (src.CreateBlackoutCalendar == null)
                continue;

            sharedCalendar ??= destSchedule.JobBlackoutCalendar;
            if (sharedCalendar != null)
                destSchedule.JobBlackoutCalendar = sharedCalendar;
        }
    }

    /// <summary>Assigns ids to nested blackout calendars/windows on a definition create, reusing ids for shared calendar instances.</summary>
    public static void AssignNestedBlackoutCalendarIds(JobDefinition definition)
    {
        var calendarIds = new Dictionary<JobBlackoutCalendar, Guid>();
        foreach (var schedule in definition.JobSchedules) {
            var calendar = schedule.JobBlackoutCalendar;
            if (calendar == null)
                continue;

            if (!calendarIds.TryGetValue(calendar, out var calendarId)) {
                calendarId = calendar.Id == default ? LyoGuid.CreateCombPostgres() : calendar.Id;
                calendar.Id = calendarId;
                calendarIds[calendar] = calendarId;
            }
            else
                calendar.Id = calendarId;

            foreach (var window in calendar.JobBlackoutWindows) {
                if (window.Id == default)
                    window.Id = LyoGuid.CreateCombPostgres();

                window.JobBlackoutCalendarId = calendarId;
            }

            schedule.JobBlackoutCalendarId = calendarId;
        }
    }

    /// <summary>Assigns ids to a nested blackout calendar on a standalone schedule create.</summary>
    public static void AssignNestedBlackoutCalendarIds(JobSchedule schedule)
    {
        var calendar = schedule.JobBlackoutCalendar;
        if (calendar == null)
            return;

        if (calendar.Id == default)
            calendar.Id = LyoGuid.CreateCombPostgres();

        schedule.JobBlackoutCalendarId = calendar.Id;

        foreach (var window in calendar.JobBlackoutWindows) {
            if (window.Id == default)
                window.Id = LyoGuid.CreateCombPostgres();

            window.JobBlackoutCalendarId = calendar.Id;
        }
    }

    private static bool HasScheduleBlackoutOverride(JobDefinitionReq definition, JobScheduleReq schedule)
        => schedule.JobBlackoutCalendarId.HasValue
            || schedule.CreateBlackoutCalendar != null && !ReferenceEquals(schedule.CreateBlackoutCalendar, definition.CreateBlackoutCalendar);
}
