using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Builders;

/// <summary>Fluent builder for <see cref="JobBlackoutCalendarReq" /> — reusable do-not-run windows for job schedules.</summary>
public class JobBlackoutCalendarBuilder
{
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

    public JobBlackoutCalendarBuilder AddBlackoutWindow(string name, DayFlags days, string startTime, string endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
        => AddBlackoutWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

    public JobBlackoutCalendarBuilder AddBlackoutWindow(string name, DayFlags days, TimeOnly startTime, TimeOnly endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
    {
        _calendar.CreateBlackoutWindows.Add(new() {
            Name = name,
            DayFlags = days,
            StartTime = startTime,
            EndTime = endTime,
            Policy = policy,
            Enabled = enabled
        });
        return this;
    }

    public JobBlackoutCalendarReq Build() => _calendar;

    public static JobBlackoutCalendarBuilder New(string name, string? description = null) => new(name, description);
}
