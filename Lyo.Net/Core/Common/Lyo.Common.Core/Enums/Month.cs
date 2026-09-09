using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum Month
{
    /// <summary>Not known or omitted</summary>
    [Description("Unknown")]
    Unk = 0,

    /// <summary>First month of the year</summary>
    [Description("Jan")]
    Jan = 1,

    /// <summary>Second month of the year</summary>
    [Description("Feb")]
    Feb = 2,

    /// <summary>Third month of the year</summary>
    [Description("Mar")]
    Mar = 3,

    /// <summary>Fourth month of the year</summary>
    [Description("Apr")]
    Apr = 4,

    /// <summary>Fifth month of the year</summary>
    [Description("May")]
    May = 5,

    /// <summary>Sixth month of the year</summary>
    [Description("Jun")]
    Jun = 6,

    /// <summary>Seventh month of the year</summary>
    [Description("Jul")]
    Jul = 7,

    /// <summary>Eighth month of the year</summary>
    [Description("Aug")]
    Aug = 8,

    /// <summary>Ninth month of the year</summary>
    [Description("Sep")]
    Sep = 9,

    /// <summary>Tenth month of the year</summary>
    [Description("Oct")]
    Oct = 10,

    /// <summary>Eleventh month of the year</summary>
    [Description("Nov")]
    Nov = 11,

    /// <summary>Twelfth month of the year</summary>
    [Description("Dec")]
    Dec = 12
}