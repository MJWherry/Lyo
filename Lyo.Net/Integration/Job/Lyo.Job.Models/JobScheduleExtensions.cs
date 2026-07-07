using Lyo.Exceptions;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Models;

/// <summary>Extensions for converting job schedule models to ScheduleDefinition for use with Lyo.Scheduler.</summary>
public static class JobScheduleExtensions
{
    /// <summary>Converts JobScheduleReq to ScheduleDefinition. The schedule's <c>TimeZoneId</c> (when set and valid) becomes the definition's time zone.</summary>
    public static ScheduleDefinition ToScheduleDefinition(this JobScheduleReq req)
    {
        ArgumentHelpers.ThrowIfNull(req);
        return new(
            req.Type, req.DayFlags, req.MonthFlags, req.Times, req.StartTime, req.EndTime, req.IntervalMinutes, null, ResolveTimeZone(req.TimeZoneId), req.Enabled,
            req.Description, req.CronExpression);
    }

    /// <summary>Converts JobScheduleRes to ScheduleDefinition. The schedule's <c>TimeZoneId</c> (when set and valid) becomes the definition's time zone.</summary>
    public static ScheduleDefinition ToScheduleDefinition(this JobScheduleRes res)
    {
        ArgumentHelpers.ThrowIfNull(res);
        return new(
            res.Type, res.DayFlags, res.MonthFlags, res.Times, res.StartTime, res.EndTime, res.IntervalMinutes, null, ResolveTimeZone(res.TimeZoneId), res.Enabled,
            res.Description, res.CronExpression);
    }

    /// <summary>Resolves a time zone id to a <see cref="TimeZoneInfo" />, returning null for missing or unknown ids.</summary>
    public static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        try {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) {
            return null;
        }
    }
}