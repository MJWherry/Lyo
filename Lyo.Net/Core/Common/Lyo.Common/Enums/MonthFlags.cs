using System.ComponentModel;

namespace Lyo.Common.Enums;

[Flags]
public enum MonthFlags
{
    /// <summary>No months selected.</summary>
    [Description("None")]
    None = 0,

    /// <summary>January</summary>
    [Description("Jan")]
    Jan = 1 << 0,

    /// <summary>February</summary>
    [Description("Feb")]
    Feb = 1 << 1,

    /// <summary>March</summary>
    [Description("Mar")]
    Mar = 1 << 2,

    /// <summary>April</summary>
    [Description("Apr")]
    Apr = 1 << 3,

    /// <summary>May</summary>
    [Description("May")]
    May = 1 << 4,

    /// <summary>June</summary>
    [Description("Jun")]
    Jun = 1 << 5,

    /// <summary>July</summary>
    [Description("Jul")]
    Jul = 1 << 6,

    /// <summary>August</summary>
    [Description("Aug")]
    Aug = 1 << 7,

    /// <summary>September</summary>
    [Description("Sep")]
    Sep = 1 << 8,

    /// <summary>October</summary>
    [Description("Oct")]
    Oct = 1 << 9,

    /// <summary>November</summary>
    [Description("Nov")]
    Nov = 1 << 10,

    /// <summary>December</summary>
    [Description("Dec")]
    Dec = 1 << 11,

    /// <summary>All 12 months: January to December</summary>
    [Description("All 12 months: January to December")]
    EveryMonth = Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec,

    /// <summary>Start of each quarter: January, April, July, October</summary>
    [Description("Start of each quarter: January, April, July, October")]
    QuarterlyStart = Jan | Apr | Jul | Oct,

    /// <summary>End of each quarter: March, June, September, December</summary>
    [Description("End of each quarter: March, June, September, December")]
    QuarterlyEnd = Mar | Jun | Sep | Dec
}