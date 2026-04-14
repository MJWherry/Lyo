using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum Day
{
    /// <summary>Unknown or not specified</summary>
    [Description("Unknown")]
    Unk = 0,

    /// <summary>Sunday</summary>
    [Description("Sun")]
    Sun = 1,

    /// <summary>Monday</summary>
    [Description("Mon")]
    Mon = 2,

    /// <summary>Tuesday</summary>
    [Description("Tue")]
    Tue = 3,

    /// <summary>Wednesday</summary>
    [Description("Wed")]
    Wed = 4,

    /// <summary>Thursday</summary>
    [Description("Thu")]
    Thu = 5,

    /// <summary>Friday</summary>
    [Description("Fri")]
    Fri = 6,

    /// <summary>Saturday</summary>
    [Description("Sat")]
    Sat = 7
}