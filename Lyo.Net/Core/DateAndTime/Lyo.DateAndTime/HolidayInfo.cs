using System.Reflection;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>Represents metadata for a U.S. holiday and can resolve its calendar date for a given year.</summary>
public sealed record HolidayInfo(
    string Name,
    string Slug,
    string Description,
    bool IsFederal,
    bool IsObservedWhenWeekend,
    HolidayDateRule DateRule,
    Month Month,
    int DayOfMonth,
    DayOfWeek DayOfWeek,
    int Occurrence,
    string[] Aliases)
{
    public static readonly HolidayInfo Unknown = new(
        "Unknown", "unknown", "Unknown or unspecified holiday.", false, false, HolidayDateRule.Unknown, Month.Unk, 0, default, 0, ["unknown", "unspecified"]);

    public static readonly HolidayInfo NewYearsDay = new(
        "New Year's Day", "new-years-day", "Celebrates the first day of the calendar year.", true, true, HolidayDateRule.FixedDate, Month.Jan, 1, default, 0,
        ["new year", "new years", "new year's"]);

    public static readonly HolidayInfo MartinLutherKingJrDay = new(
        "Martin Luther King Jr. Day", "martin-luther-king-jr-day", "Honors the life and legacy of Dr. Martin Luther King Jr.", true, false, HolidayDateRule.NthWeekdayOfMonth,
        Month.Jan, 0, DayOfWeek.Monday, 3, ["mlk day", "martin luther king day"]);

    public static readonly HolidayInfo ValentinesDay = new(
        "Valentine's Day", "valentines-day", "Popular observance focused on love and affection.", false, false, HolidayDateRule.FixedDate, Month.Feb, 14, default, 0,
        ["valentine day", "saint valentine's day"]);

    public static readonly HolidayInfo PresidentsDay = new(
        "Presidents' Day", "presidents-day", "Federal holiday honoring George Washington and commonly all U.S. presidents.", true, false, HolidayDateRule.NthWeekdayOfMonth,
        Month.Feb, 0, DayOfWeek.Monday, 3, ["washington's birthday", "presidents day"]);

    public static readonly HolidayInfo StPatricksDay = new(
        "St. Patrick's Day", "st-patricks-day", "Cultural and religious observance associated with Irish heritage.", false, false, HolidayDateRule.FixedDate, Month.Mar, 17,
        default, 0, ["saint patrick's day", "st patricks day"]);

    public static readonly HolidayInfo EasterSunday = new(
        "Easter Sunday", "easter-sunday", "Christian holiday celebrating the resurrection of Jesus Christ.", false, false, HolidayDateRule.Unknown, Month.Unk, 0, default, 0,
        ["easter"]);

    public static readonly HolidayInfo MothersDay = new(
        "Mother's Day", "mothers-day", "Celebrates mothers and motherhood.", false, false, HolidayDateRule.NthWeekdayOfMonth, Month.May, 0, DayOfWeek.Sunday, 2, ["mothers day"]);

    public static readonly HolidayInfo MemorialDay = new(
        "Memorial Day", "memorial-day", "Honors U.S. military personnel who died in service.", true, false, HolidayDateRule.LastWeekdayOfMonth, Month.May, 0, DayOfWeek.Monday, 0,
        ["decoration day"]);

    public static readonly HolidayInfo Juneteenth = new(
        "Juneteenth National Independence Day", "juneteenth", "Commemorates the end of slavery in the United States.", true, true, HolidayDateRule.FixedDate, Month.Jun, 19,
        default, 0, ["juneteenth national independence day"]);

    public static readonly HolidayInfo FathersDay = new(
        "Father's Day", "fathers-day", "Celebrates fathers and fatherhood.", false, false, HolidayDateRule.NthWeekdayOfMonth, Month.Jun, 0, DayOfWeek.Sunday, 3, ["fathers day"]);

    public static readonly HolidayInfo IndependenceDay = new(
        "Independence Day", "independence-day", "Commemorates the adoption of the Declaration of Independence.", true, true, HolidayDateRule.FixedDate, Month.Jul, 4, default, 0,
        ["fourth of july", "4th of july"]);

    public static readonly HolidayInfo LaborDay = new(
        "Labor Day", "labor-day", "Celebrates the contributions of workers and the labor movement.", true, false, HolidayDateRule.NthWeekdayOfMonth, Month.Sep, 0, DayOfWeek.Monday,
        1, ["labour day"]);

    public static readonly HolidayInfo ColumbusDay = new(
        "Columbus Day", "columbus-day", "Federal holiday observed on the second Monday in October.", true, false, HolidayDateRule.NthWeekdayOfMonth, Month.Oct, 0, DayOfWeek.Monday,
        2, ["indigenous peoples' day", "indigenous peoples day"]);

    public static readonly HolidayInfo Halloween = new(
        "Halloween", "halloween", "Popular cultural holiday observed on October 31.", false, false, HolidayDateRule.FixedDate, Month.Oct, 31, default, 0, ["all hallows' eve"]);

    public static readonly HolidayInfo VeteransDay = new(
        "Veterans Day", "veterans-day", "Honors U.S. military veterans.", true, true, HolidayDateRule.FixedDate, Month.Nov, 11, default, 0, ["armistice day"]);

    public static readonly HolidayInfo ThanksgivingDay = new(
        "Thanksgiving Day", "thanksgiving-day", "National day of thanksgiving observed on the fourth Thursday in November.", true, false, HolidayDateRule.NthWeekdayOfMonth,
        Month.Nov, 0, DayOfWeek.Thursday, 4, ["thanksgiving"]);

    public static readonly HolidayInfo ChristmasDay = new(
        "Christmas Day", "christmas-day", "Christian and cultural holiday celebrating the birth of Jesus Christ.", true, true, HolidayDateRule.FixedDate, Month.Dec, 25, default, 0,
        ["christmas", "xmas"]);

    public static readonly HolidayInfo NewYearsEve = new(
        "New Year's Eve", "new-years-eve", "Marks the final day of the calendar year.", false, false, HolidayDateRule.FixedDate, Month.Dec, 31, default, 0,
        ["new year eve", "new years eve"]);

    private static readonly Dictionary<string, HolidayInfo> _byName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HolidayInfo> _bySlug = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HolidayInfo> _byAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<HolidayInfo> _all = [];

    public string CanonicalName => Slug;

    /// <summary>Gets all registered holiday metadata records.</summary>
    public static IReadOnlyList<HolidayInfo> All => _all;

    /// <summary>Gets all holidays marked as U.S. federal holidays.</summary>
    public static IReadOnlyList<HolidayInfo> FederalHolidays => _all.Where(i => i.IsFederal).ToArray();

    static HolidayInfo()
    {
        var fields = typeof(HolidayInfo).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(HolidayInfo))
            .Select(f => (HolidayInfo)f.GetValue(null)!)
            .ToList();

        foreach (var holiday in fields) {
            _all.Add(holiday);
            _byName[Normalize(holiday.Name)] = holiday;
            _bySlug[Normalize(holiday.Slug)] = holiday;
            _byAlias[Normalize(holiday.Name)] = holiday;
            _byAlias[Normalize(holiday.Slug)] = holiday;
            foreach (var alias in holiday.Aliases.Where(i => !string.IsNullOrWhiteSpace(i)))
                _byAlias[Normalize(alias)] = holiday;
        }
    }

    public static HolidayInfo FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Unknown;

        return _byName.TryGetValue(Normalize(name), out var holiday) ? holiday : FromAlias(name);
    }

    public static HolidayInfo FromSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Unknown;

        return _bySlug.TryGetValue(Normalize(slug), out var holiday) ? holiday : Unknown;
    }

    public static HolidayInfo FromAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return Unknown;

        return _byAlias.TryGetValue(Normalize(alias), out var holiday) ? holiday : Unknown;
    }

    public DateTime GetDate(int year)
    {
        switch (DateRule) {
            case HolidayDateRule.FixedDate:
                return new(year, (int)Month, DayOfMonth);
            case HolidayDateRule.NthWeekdayOfMonth:
                return GetNthWeekdayOfMonth(year, (int)Month, DayOfWeek, Occurrence);
            case HolidayDateRule.LastWeekdayOfMonth:
                return GetLastWeekdayOfMonth(year, (int)Month, DayOfWeek);
            default:
                if (ReferenceEquals(this, EasterSunday))
                    return GetWesternEasterSunday(year);

                throw new InvalidOperationException($"Holiday '{Name}' does not have a supported date rule.");
        }
    }

    public DateTime GetObservedDate(int year)
    {
        var date = GetDate(year);
        if (!IsObservedWhenWeekend)
            return date;

        switch (date.DayOfWeek) {
            case DayOfWeek.Saturday:
                return date.AddDays(-1);
            case DayOfWeek.Sunday:
                return date.AddDays(1);
            default:
                return date;
        }
    }

    public bool OccursOn(DateTime date, bool includeObservedDate = true)
    {
        var holidayDate = GetDate(date.Year).Date;
        if (holidayDate == date.Date)
            return true;

        return includeObservedDate && GetObservedDate(date.Year).Date == date.Date;
    }

    public static HolidayInfo? FromDate(DateTime date, bool includeObservedDate = true)
        => _all.FirstOrDefault(i => !ReferenceEquals(i, Unknown) && i.OccursOn(date, includeObservedDate));

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int occurrence)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(occurrence, nameof(occurrence));
        var date = new DateTime(year, month, 1);
        while (date.DayOfWeek != dayOfWeek)
            date = date.AddDays(1);

        return date.AddDays((occurrence - 1) * 7);
    }

    private static DateTime GetLastWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        var date = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (date.DayOfWeek != dayOfWeek)
            date = date.AddDays(-1);

        return date;
    }

    // Meeus/Jones/Butcher algorithm for Gregorian Easter.
    private static DateTime GetWesternEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new(year, month, day);
    }
}