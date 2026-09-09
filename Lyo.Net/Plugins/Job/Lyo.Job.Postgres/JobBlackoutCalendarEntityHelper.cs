using Lyo.Common.Core.Identifiers;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;

namespace Lyo.Job.Postgres;

/// <summary>Shared blackout-calendar persistence helpers used when creating definitions and schedules.</summary>
internal static class JobBlackoutCalendarEntityHelper
{
    /// <summary>
    /// After mapping a <see cref="JobDefinitionReq" />, makes sure schedules that inherit the definition default share one <see cref="JobBlackoutCalendar" /> entity, while
    /// schedules with explicit overrides keep their own. Maps definition-level <see cref="JobDefinitionReq.CreateBlackoutCalendar" /> when it was not cascaded onto schedules.
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

            // Prefer a calendar already built from a cascaded or structurally matching schedule CreateBlackoutCalendar.
            // Otherwise map the definition-level calendar once (definition-only API payloads).
            sharedCalendar ??= destSchedule.JobBlackoutCalendar ??
                destSchedules.Select(s => s.JobBlackoutCalendar).FirstOrDefault(c => c != null) ?? JobLyoMapper.ReqToNew(src.CreateBlackoutCalendar);

            destSchedule.JobBlackoutCalendar = sharedCalendar;
        }
    }

    /// <summary>Assigns ids to nested blackout calendars and windows on a definition create, reusing ids for shared calendar instances.</summary>
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
                JobBlackoutWindowWriteValidator.ValidateAndNormalize(window);
            }

            schedule.JobBlackoutCalendarId = calendarId;
        }
    }

    /// <summary>Assigns ids to the nested blackout calendar when creating a standalone schedule.</summary>
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
            JobBlackoutWindowWriteValidator.ValidateAndNormalize(window);
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

        // After JSON deserialize, shared calendars become separate instances that still have identical content.
        if (definition.CreateBlackoutCalendar is not null && StructurallyEquals(schedule.CreateBlackoutCalendar, definition.CreateBlackoutCalendar))
            return false;

        return true;
    }

    internal static bool StructurallyEquals(JobBlackoutCalendarReq a, JobBlackoutCalendarReq b)
    {
        if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal) || !string.Equals(a.Description, b.Description, StringComparison.Ordinal) || a.Enabled != b.Enabled ||
            a.CreateBlackoutWindows.Count != b.CreateBlackoutWindows.Count)
            return false;

        for (var i = 0; i < a.CreateBlackoutWindows.Count; i++) {
            var wa = a.CreateBlackoutWindows[i];
            var wb = b.CreateBlackoutWindows[i];
            if (!string.Equals(wa.Name, wb.Name, StringComparison.Ordinal) || wa.DayFlags != wb.DayFlags || wa.StartTime != wb.StartTime || wa.EndTime != wb.EndTime ||
                wa.Policy != wb.Policy || wa.Enabled != wb.Enabled || wa.StartDateUtc != wb.StartDateUtc || wa.EndDateUtc != wb.EndDateUtc ||
                !string.Equals(wa.HolidaySlug, wb.HolidaySlug, StringComparison.Ordinal) || wa.IncludeObservedDate != wb.IncludeObservedDate || wa.MonthFlags != wb.MonthFlags ||
                !DaysOfMonthEquals(wa.DaysOfMonth, wb.DaysOfMonth))
                return false;
        }

        return true;
    }

    private static bool DaysOfMonthEquals(List<int>? a, List<int>? b)
    {
        if (a is null || a.Count == 0)
            return b is null || b.Count == 0;

        if (b is null || b.Count != a.Count)
            return false;

        return a.OrderBy(d => d).SequenceEqual(b.OrderBy(d => d));
    }
}