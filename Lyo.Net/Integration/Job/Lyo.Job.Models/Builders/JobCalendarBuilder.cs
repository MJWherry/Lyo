using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Builders;

public class JobCalendarBuilder
{
    private readonly JobCalendarReq _calendar = new();

    public JobCalendarBuilder(string name, string? description = null)
    {
        _calendar.Name = name;
        _calendar.Description = description;
    }

    public JobCalendarBuilder Enabled(bool enabled = true)
    {
        _calendar.Enabled = enabled;
        return this;
    }

    public JobCalendarBuilder AddWindow(string name, DayFlags days, string startTime, string endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
        => AddWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

    public JobCalendarBuilder AddWindow(string name, DayFlags days, TimeOnly startTime, TimeOnly endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
    {
        _calendar.CreateWindows.Add(new() {
            Name = name,
            DayFlags = days,
            StartTime = startTime,
            EndTime = endTime,
            Policy = policy,
            Enabled = enabled
        });
        return this;
    }

    public JobCalendarReq Build() => _calendar;

    public static JobCalendarBuilder New(string name, string? description = null) => new(name, description);
}
