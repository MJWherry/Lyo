using System.ComponentModel.DataAnnotations;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Postgres;

/// <summary>Write-time checks for blackout windows so unknown holiday slugs and invalid calendar days fail on save.</summary>
internal static class JobBlackoutWindowWriteValidator
{
    /// <summary>Normalizes exclusive window kinds and throws when the stored shape is invalid.</summary>
    public static void ValidateAndNormalize(JobBlackoutWindow window)
    {
        var errors = new List<string>();
        if (!string.IsNullOrWhiteSpace(window.HolidaySlug)) {
            var holiday = HolidayInfo.FromSlug(window.HolidaySlug);
            if (ReferenceEquals(holiday, HolidayInfo.Unknown))
                errors.Add($"Holiday slug '{window.HolidaySlug}' is not a known holiday.");
            else
                window.HolidaySlug = holiday.Slug;

            window.DayFlags = nameof(DayFlags.None);
            window.MonthFlags = null;
            window.DaysOfMonth = null;
            window.StartDateUtc = null;
            window.EndDateUtc = null;
        }
        else if (window.DaysOfMonth is { Count: > 0 }) {
            var months = TypeConversion.EnumOrDefault(window.MonthFlags, MonthFlags.None);
            if (months == MonthFlags.None)
                errors.Add("Calendar-day windows require at least one month.");

            if (window.DaysOfMonth.Any(d => d is < 1 or > 31))
                errors.Add("Days of the month must be between 1 and 31.");

            window.HolidaySlug = null;
            window.IncludeObservedDate = false;
            window.DayFlags = nameof(DayFlags.None);
            window.DaysOfMonth = window.DaysOfMonth.Distinct().OrderBy(d => d).ToList();
        }
        else {
            window.HolidaySlug = null;
            window.IncludeObservedDate = false;
            window.MonthFlags = null;
            window.DaysOfMonth = null;
        }

        if (errors.Count > 0)
            throw new ValidationException(string.Join(" ", errors));
    }

    /// <summary>Validates every window on a calendar.</summary>
    public static void ValidateAndNormalize(JobBlackoutCalendar calendar)
    {
        foreach (var window in calendar.JobBlackoutWindows)
            ValidateAndNormalize(window);
    }
}
