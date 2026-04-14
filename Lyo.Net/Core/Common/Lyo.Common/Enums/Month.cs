using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum Month
{
    /// <summary>Unknown or not specified</summary>
    [Description("Unknown")]
    Unk = 0,

    /// <summary>January</summary>
    [Description("Jan")]
    Jan = 1,

    /// <summary>February</summary>
    [Description("Feb")]
    Feb = 2,

    /// <summary>March</summary>
    [Description("Mar")]
    Mar = 3,

    /// <summary>April</summary>
    [Description("Apr")]
    Apr = 4,

    /// <summary>May</summary>
    [Description("May")]
    May = 5,

    /// <summary>June</summary>
    [Description("Jun")]
    Jun = 6,

    /// <summary>July</summary>
    [Description("Jul")]
    Jul = 7,

    /// <summary>August</summary>
    [Description("Aug")]
    Aug = 8,

    /// <summary>September</summary>
    [Description("Sep")]
    Sep = 9,

    /// <summary>October</summary>
    [Description("Oct")]
    Oct = 10,

    /// <summary>November</summary>
    [Description("Nov")]
    Nov = 11,

    /// <summary>December</summary>
    [Description("Dec")]
    Dec = 12
}