using Lyo.Exceptions;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Models;

/// <summary>Extensions for converting job schedule models to ScheduleDefinition for use with Lyo.Scheduler.</summary>
public static class JobScheduleExtensions
{
    /// <summary>Converts JobScheduleReq to ScheduleDefinition. Job schedules support SetTimes and Interval only; OneShot and TimeZone are null.</summary>
    public static ScheduleDefinition ToScheduleDefinition(this JobScheduleReq req)
    {
        ArgumentHelpers.ThrowIfNull(req, nameof(req));
        return new(req.Type, req.DayFlags, req.MonthFlags, req.Times, req.StartTime, req.EndTime, req.IntervalMinutes, null, null, req.Enabled, req.Description);
    }

    /// <summary>Converts JobScheduleRes to ScheduleDefinition. Job schedules support SetTimes and Interval only; OneShot and TimeZone are null.</summary>
    public static ScheduleDefinition ToScheduleDefinition(this JobScheduleRes res)
    {
        ArgumentHelpers.ThrowIfNull(res, nameof(res));
        return new(res.Type, res.DayFlags, res.MonthFlags, res.Times, res.StartTime, res.EndTime, res.IntervalMinutes, null, null, res.Enabled, res.Description);
    }
}