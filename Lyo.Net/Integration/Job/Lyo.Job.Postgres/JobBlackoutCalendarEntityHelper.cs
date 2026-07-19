using Lyo.Common.Identifiers;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;

namespace Lyo.Job.Postgres;

/// <summary>Shared blackout-calendar persistence helpers for definition and schedule creates.</summary>
internal static class JobBlackoutCalendarEntityHelper
{
    /// <summary>
    /// After mapping a <see cref="JobDefinitionReq" />, ensures schedules inheriting the definition default share one
    /// <see cref="JobBlackoutCalendar" /> entity and schedules with explicit overrides keep their own.
    /// Maps definition-level <see cref="JobDefinitionReq.CreateBlackoutCalendar" /> when it was not cascaded onto schedules.
    /// Treats schedule calendars as inherited when they structurally match the definition calendar (JSON round-trip).
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

            // Prefer a calendar already built from a cascaded/structurally-matching schedule CreateBlackoutCalendar;
            // otherwise map the definition-level calendar once (definition-only API payloads).
            sharedCalendar ??= destSchedule.JobBlackoutCalendar
                ?? destSchedules.Select(s => s.JobBlackoutCalendar).FirstOrDefault(c => c != null)
                ?? JobLyoMapper.ReqToNew(src.CreateBlackoutCalendar);
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
    {
        if (schedule.JobBlackoutCalendarId.HasValue)
            return true;

        if (schedule.CreateBlackoutCalendar is null)
            return false;

        if (ReferenceEquals(schedule.CreateBlackoutCalendar, definition.CreateBlackoutCalendar))
            return false;

        // After JSON deserialize, shared calendars become distinct instances with identical content.
        if (definition.CreateBlackoutCalendar is not null
            && StructurallyEquals(schedule.CreateBlackoutCalendar, definition.CreateBlackoutCalendar))
            return false;

        return true;
    }

    internal static bool StructurallyEquals(JobBlackoutCalendarReq a, JobBlackoutCalendarReq b)
    {
        if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal)
            || !string.Equals(a.Description, b.Description, StringComparison.Ordinal)
            || a.Enabled != b.Enabled
            || a.CreateBlackoutWindows.Count != b.CreateBlackoutWindows.Count)
            return false;

        for (var i = 0; i < a.CreateBlackoutWindows.Count; i++) {
            var wa = a.CreateBlackoutWindows[i];
            var wb = b.CreateBlackoutWindows[i];
            if (!string.Equals(wa.Name, wb.Name, StringComparison.Ordinal)
                || wa.DayFlags != wb.DayFlags
                || wa.StartTime != wb.StartTime
                || wa.EndTime != wb.EndTime
                || wa.Policy != wb.Policy
                || wa.Enabled != wb.Enabled
                || wa.StartDateUtc != wb.StartDateUtc
                || wa.EndDateUtc != wb.EndDateUtc)
                return false;
        }

        return true;
    }
}
