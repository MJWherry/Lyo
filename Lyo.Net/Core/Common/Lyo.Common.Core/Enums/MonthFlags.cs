using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

[Flags]
public enum MonthFlags
{
    /// <summary>Empty set — no month bits.</summary>
    [Description("None")]
    None = 0,

    /// <summary>January bit</summary>
    [Description("Jan")]
    Jan = 1 << 0,

    /// <summary>February bit</summary>
    [Description("Feb")]
    Feb = 1 << 1,

    /// <summary>March bit</summary>
    [Description("Mar")]
    Mar = 1 << 2,

    /// <summary>April bit</summary>
    [Description("Apr")]
    Apr = 1 << 3,

    /// <summary>May bit</summary>
    [Description("May")]
    May = 1 << 4,

    /// <summary>June bit</summary>
    [Description("Jun")]
    Jun = 1 << 5,

    /// <summary>July bit</summary>
    [Description("Jul")]
    Jul = 1 << 6,

    /// <summary>August bit</summary>
    [Description("Aug")]
    Aug = 1 << 7,

    /// <summary>September bit</summary>
    [Description("Sep")]
    Sep = 1 << 8,

    /// <summary>October bit</summary>
    [Description("Oct")]
    Oct = 1 << 9,

    /// <summary>November bit</summary>
    [Description("Nov")]
    Nov = 1 << 10,

    /// <summary>December bit</summary>
    [Description("Dec")]
    Dec = 1 << 11,

    /// <summary>Every month bit: January through December</summary>
    [Description("All 12 months: January to December")]
    EveryMonth = Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec,

    /// <summary>Quarter-start months: January, April, July, October</summary>
    [Description("Start of each quarter: January, April, July, October")]
    QuarterlyStart = Jan | Apr | Jul | Oct,

    /// <summary>Quarter-end months: March, June, September, December</summary>
    [Description("End of each quarter: March, June, September, December")]
    QuarterlyEnd = Mar | Jun | Sep | Dec
}