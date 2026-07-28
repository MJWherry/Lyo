#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Enums;
using Lyo.DateAndTime;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

/// <summary>Fluent builder for <see cref="JobBlackoutCalendarReq" /> — reusable do-not-run windows for job schedules.</summary>
public class JobBlackoutCalendarBuilder
{
    private const int DefaultHolidayYearSpan = 10;

    private readonly JobBlackoutCalendarReq _calendar = new();

    public JobBlackoutCalendarBuilder(string name, string? description = null)
    {
        _calendar.Name = name;
        _calendar.Description = description;
    }

    public JobBlackoutCalendarBuilder Enabled(bool enabled = true)
    {
        _calendar.Enabled = enabled;
        return this;
    }

    public JobBlackoutCalendarBuilder AddBlackoutWindow(
        string name,
        DayFlags days,
        string startTime,
        string endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool enabled = true)
        => AddBlackoutWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

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

    /// <summary>Expands a <see cref="HolidayInfo" /> into concrete dated blackout windows (default: current year + 9 years).</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        HolidayInfo holiday,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
    {
        var fromYear = DateTime.UtcNow.Year;
        return AddBlackoutHoliday(holiday, fromYear, fromYear + DefaultHolidayYearSpan - 1, startTime, endTime, policy, includeObservedDate, enabled);
    }

    /// <summary>Expands a <see cref="HolidayInfo" /> into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        HolidayInfo holiday,
        int fromYear,
        int toYear,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
        => AddBlackoutHoliday(holiday, fromYear, toYear, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>Expands a <see cref="HolidayInfo" /> into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        HolidayInfo holiday,
        int fromYear,
        int toYear,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
        => AddBlackoutHoliday(holiday.Name, holiday, fromYear, toYear, startTime, endTime, policy, includeObservedDate, enabled);

    /// <summary>Expands a <see cref="HolidayInfo" /> into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        string name,
        HolidayInfo holiday,
        int fromYear,
        int toYear,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
        => AddBlackoutHoliday(name, holiday, fromYear, toYear, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>Expands a <see cref="HolidayInfo" /> into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHoliday(
        string name,
        HolidayInfo holiday,
        int fromYear,
        int toYear,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
    {
        ValidateHoliday(holiday);
        foreach (var window in CreateHolidayWindows(name, holiday, fromYear, toYear, startTime, endTime, policy, includeObservedDate, enabled))
            _calendar.CreateBlackoutWindows.Add(window);

        return this;
    }

    /// <summary>Expands every U.S. federal holiday in <see cref="HolidayInfo.FederalHolidays" /> into concrete dated blackout windows.</summary>
    public JobBlackoutCalendarBuilder AddFederalHolidayBlackouts(
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
        => AddBlackoutHolidays(HolidayInfo.FederalHolidays, startTime, endTime, policy, includeObservedDate, enabled);

    /// <summary>Expands each holiday into concrete dated blackout windows (default: current year + 9 years).</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHolidays(
        IEnumerable<HolidayInfo> holidays,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
    {
        var fromYear = DateTime.UtcNow.Year;
        return AddBlackoutHolidays(holidays, fromYear, fromYear + DefaultHolidayYearSpan - 1, startTime, endTime, policy, includeObservedDate, enabled);
    }

    /// <summary>Expands each holiday into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHolidays(
        IEnumerable<HolidayInfo> holidays,
        int fromYear,
        int toYear,
        string startTime = "00:00",
        string endTime = "23:59",
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
        => AddBlackoutHolidays(holidays, fromYear, toYear, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, includeObservedDate, enabled);

    /// <summary>Expands each holiday into concrete dated blackout windows for each year in the inclusive range.</summary>
    public JobBlackoutCalendarBuilder AddBlackoutHolidays(
        IEnumerable<HolidayInfo> holidays,
        int fromYear,
        int toYear,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool includeObservedDate = true,
        bool enabled = true)
    {
        ArgumentHelpers.ThrowIfNull(holidays);
        foreach (var holiday in holidays)
            AddBlackoutHoliday(holiday, fromYear, toYear, startTime, endTime, policy, includeObservedDate, enabled);

        return this;
    }

    public JobBlackoutCalendarReq Build() => _calendar;

    public static JobBlackoutCalendarBuilder New(string name, string? description = null) => new(name, description);

    private static void ValidateHoliday(HolidayInfo holiday)
    {
        ArgumentHelpers.ThrowIfNull(holiday);
        ArgumentHelpers.ThrowIf(ReferenceEquals(holiday, HolidayInfo.Unknown), "Holiday must be a known holiday.", nameof(holiday));
    }

    private static IEnumerable<JobBlackoutWindowReq> CreateHolidayWindows(
        string name,
        HolidayInfo holiday,
        int fromYear,
        int toYear,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy,
        bool includeObservedDate,
        bool enabled)
    {
        ArgumentHelpers.ThrowIf(toYear < fromYear, $"{nameof(toYear)} must be greater than or equal to {nameof(fromYear)}.");
        for (var year = fromYear; year <= toYear; year++) {
            var date = (includeObservedDate ? holiday.GetObservedDate(year) : holiday.GetDate(year)).Date;
            var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            yield return new() {
                Name = $"{name} ({year})",
                DayFlags = DayFlags.EveryDay,
                StartDateUtc = dateUtc,
                EndDateUtc = dateUtc,
                StartTime = startTime,
                EndTime = endTime,
                Policy = policy,
                Enabled = enabled
            };
        }
    }
}