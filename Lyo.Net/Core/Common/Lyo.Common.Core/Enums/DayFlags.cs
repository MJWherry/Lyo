using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

[Flags]
public enum DayFlags
{
    /// <summary>Empty set — no weekday bits</summary>
    [Description("None")]
    None = 0,

    /// <summary>Sunday bit</summary>
    [Description("Sunday")]
    Sun = 1 << 0,

    /// <summary>Monday bit</summary>
    [Description("Monday")]
    Mon = 1 << 1,

    /// <summary>Tuesday bit</summary>
    [Description("Tuesday")]
    Tue = 1 << 2,

    /// <summary>Wednesday bit</summary>
    [Description("Wednesday")]
    Wed = 1 << 3,

    /// <summary>Thursday bit</summary>
    [Description("Thursday")]
    Thu = 1 << 4,

    /// <summary>Friday bit</summary>
    [Description("Friday")]
    Fri = 1 << 5,

    /// <summary>Saturday bit</summary>
    [Description("Saturday")]
    Sat = 1 << 6,

    /// <summary>Every weekday bit: Sunday through Saturday</summary>
    [Description("All days: Sunday through Saturday")]
    EveryDay = Sun | Mon | Tue | Wed | Thu | Fri | Sat,

    /// <summary>Monday–Friday bits</summary>
    [Description("Weekdays: Monday through Friday")]
    Weekdays = Mon | Tue | Wed | Thu | Fri,

    /// <summary>Monday + Wednesday + Friday bits</summary>
    [Description("Monday, Wednesday, and Friday")]
    MonWedFri = Mon | Wed | Fri,

    /// <summary>Tuesday + Thursday bits</summary>
    [Description("Tuesday and Thursday")]
    TueThur = Tue | Thu,

    /// <summary>Saturday + Sunday bits</summary>
    [Description("Weekends: Saturday and Sunday")]
    Weekends = Sat | Sun
}