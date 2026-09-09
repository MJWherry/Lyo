using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Builders;

/// <summary>Fluent builder for <see cref="JobBlackoutCalendarReq" />: reusable do-not-run windows for job schedules.</summary>
public class JobBlackoutCalendarBuilder
{
    private readonly JobBlackoutCalendarReq _calendar = new();

    /// <summary>Starts a calendar with a display name and optional description.</summary>
    public JobBlackoutCalendarBuilder(string name, string? description = null)
    {
        _calendar.Name = name;
        _calendar.Description = description;
    }

    /// <summary>Sets whether linked schedules honor this calendar.</summary>
    public JobBlackoutCalendarBuilder Enabled(bool enabled = true)
    {
        _calendar.Enabled = enabled;
        return this;
    }

    /// <summary>Appends a recurring weekday window.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutWindow(
        string name,
        DayFlags days,
        string startTime,
        string endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool enabled = true)
        => AddBlackoutWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

    /// <summary>Appends a recurring weekday window.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutWindow(
        string name,
        DayFlags days,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool enabled = true)
    {
        _calendar.CreateBlackoutWindows.Add(
            new() {
                Name = name,
                DayFlags = days,
                StartTime = startTime,
                EndTime = endTime,
                Policy = policy,
                Enabled = enabled
            });

        return this;
    }

    /// <summary>
    /// Appends one calculated holiday window. The scheduler evaluates <see cref="HolidayInfo.OccursOn" /> each year instead of storing a dated row per year.
    /// Defaults to the holiday's calendar date, not the observed weekday.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        HolidayInfo holiday,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
        => AddBlackoutHoliday(holiday, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>
    /// Appends one calculated holiday window. The scheduler evaluates <see cref="HolidayInfo.OccursOn" /> each year instead of storing a dated row per year.
    /// Defaults to the holiday's calendar date, not the observed weekday.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        HolidayInfo holiday,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
        => AddBlackoutHoliday(holiday.Name, holiday, startTime, endTime, policy, includeObservedDate, enabled);

    /// <summary>
    /// Appends one calculated holiday window with an explicit display name. The scheduler evaluates <see cref="HolidayInfo.OccursOn" /> each year.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        string name,
        HolidayInfo holiday,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
        => AddBlackoutHoliday(name, holiday, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>
    /// Appends one calculated holiday window with an explicit display name. The scheduler evaluates <see cref="HolidayInfo.OccursOn" /> each year.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        string name,
        HolidayInfo holiday,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
    {
        ValidateHoliday(holiday);
        _calendar.CreateBlackoutWindows.Add(
            new() {
                Name = name,
                DayFlags = DayFlags.None,
                StartTime = startTime,
                EndTime = endTime,
                Policy = policy,
                Enabled = enabled,
                HolidaySlug = holiday.Slug,
                IncludeObservedDate = includeObservedDate
            });

        return this;
    }

    /// <summary>Appends one calculated holiday window for each U.S. federal holiday in <see cref="HolidayInfo.FederalHolidays" />.</summary>
    public JobBlackoutCalendarBuilder AddFederalHolidayBlackouts(
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
        => AddBlackoutHolidays(HolidayInfo.FederalHolidays, startTime, endTime, policy, includeObservedDate, enabled);

    /// <summary>Appends one calculated holiday window for each holiday in <paramref name="holidays" />.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHolidays(
        IEnumerable<HolidayInfo> holidays,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
        => AddBlackoutHolidays(holidays, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>Appends one calculated holiday window for each holiday in <paramref name="holidays" />.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHolidays(
        IEnumerable<HolidayInfo> holidays,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = false,
        bool enabled = true)
    {
        ArgumentHelpers.ThrowIfNull(holidays);
        foreach (var holiday in holidays)
            AddBlackoutHoliday(holiday, startTime, endTime, policy, includeObservedDate, enabled);

        return this;
    }

    /// <summary>
    /// Appends a calendar-day window: the given days of the month in the given months, every year unless
    /// <paramref name="startDateUtc" /> / <paramref name="endDateUtc" /> bound it.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutCalendarDays(
        string name,
        MonthFlags months,
        IReadOnlyList<int> daysOfMonth,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        bool enabled = true)
        => AddBlackoutCalendarDays(name, months, daysOfMonth, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, startDateUtc, endDateUtc, enabled);

    /// <summary>
    /// Appends a calendar-day window: the given days of the month in the given months, every year unless
    /// <paramref name="startDateUtc" /> / <paramref name="endDateUtc" /> bound it.
    /// </summary>
    public JobBlackoutCalendarBuilder AddBlackoutCalendarDays(
        string name,
        MonthFlags months,
        IReadOnlyList<int> daysOfMonth,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        bool enabled = true)
    {
        ArgumentHelpers.ThrowIfNull(daysOfMonth);
        ArgumentHelpers.ThrowIf(months == MonthFlags.None, "Calendar-day windows require at least one month.", nameof(months));
        ArgumentHelpers.ThrowIf(daysOfMonth.Count == 0, "Calendar-day windows require at least one day of the month.", nameof(daysOfMonth));
        ArgumentHelpers.ThrowIf(daysOfMonth.Any(d => d is < 1 or > 31), "Days of the month must be between 1 and 31.", nameof(daysOfMonth));
        _calendar.CreateBlackoutWindows.Add(
            new() {
                Name = name,
                DayFlags = DayFlags.None,
                StartTime = startTime,
                EndTime = endTime,
                Policy = policy,
                Enabled = enabled,
                MonthFlags = months,
                DaysOfMonth = [.. daysOfMonth],
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc
            });

        return this;
    }

    /// <summary>Returns the assembled calendar request.</summary>
    public JobBlackoutCalendarReq Build() => _calendar;

    /// <summary>Starts a calendar with a display name and optional description.</summary>
    public static JobBlackoutCalendarBuilder New(string name, string? description = null) => new(name, description);

    private static void ValidateHoliday(HolidayInfo holiday)
    {
        ArgumentHelpers.ThrowIfNull(holiday);
        ArgumentHelpers.ThrowIf(ReferenceEquals(holiday, HolidayInfo.Unknown), "Holiday must be a known holiday.", nameof(holiday));
    }
}
