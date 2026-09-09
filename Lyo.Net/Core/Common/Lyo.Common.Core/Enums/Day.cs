using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum Day
{
    /// <summary>Not known or omitted</summary>
    [Description("Unknown")]
    Unk = 0,

    /// <summary>Sunday of the week</summary>
    [Description("Sun")]
    Sun = 1,

    /// <summary>Monday of the week</summary>
    [Description("Mon")]
    Mon = 2,

    /// <summary>Tuesday of the week</summary>
    [Description("Tue")]
    Tue = 3,

    /// <summary>Wednesday of the week</summary>
    [Description("Wed")]
    Wed = 4,

    /// <summary>Thursday of the week</summary>
    [Description("Thu")]
    Thu = 5,

    /// <summary>Friday of the week</summary>
    [Description("Fri")]
    Fri = 6,

    /// <summary>Saturday of the week</summary>
    [Description("Sat")]
    Sat = 7
}