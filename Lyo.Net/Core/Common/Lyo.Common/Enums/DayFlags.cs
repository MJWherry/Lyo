using System.ComponentModel;

namespace Lyo.Common.Enums;

[Flags]
public enum DayFlags
{
    /// <summary>No days selected</summary>
    [Description("None")]
    None = 0,

    /// <summary>Sunday</summary>
    [Description("Sunday")]
    Sun = 1 << 0,

    /// <summary>Monday</summary>
    [Description("Monday")]
    Mon = 1 << 1,

    /// <summary>Tuesday</summary>
    [Description("Tuesday")]
    Tue = 1 << 2,

    /// <summary>Wednesday</summary>
    [Description("Wednesday")]
    Wed = 1 << 3,

    /// <summary>Thursday</summary>
    [Description("Thursday")]
    Thu = 1 << 4,

    /// <summary>Friday</summary>
    [Description("Friday")]
    Fri = 1 << 5,

    /// <summary>Saturday</summary>
    [Description("Saturday")]
    Sat = 1 << 6,

    /// <summary>All days: Sunday through Saturday</summary>
    [Description("All days: Sunday through Saturday")]
    EveryDay = Sun | Mon | Tue | Wed | Thu | Fri | Sat,

    /// <summary>Weekdays: Monday through Friday</summary>
    [Description("Weekdays: Monday through Friday")]
    Weekdays = Mon | Tue | Wed | Thu | Fri,

    /// <summary>Monday, Wednesday, and Friday</summary>
    [Description("Monday, Wednesday, and Friday")]
    MonWedFri = Mon | Wed | Fri,

    /// <summary>Tuesday and Thursday</summary>
    [Description("Tuesday and Thursday")]
    TueThur = Tue | Thu,

    /// <summary>Weekends: Saturday and Sunday</summary>
    [Description("Weekends: Saturday and Sunday")]
    Weekends = Sat | Sun
}