using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>IRS standard federal filing statuses.</summary>
public enum FederalFilingStatus
{
    /// <summary>Declined to answer / not disclosed</summary>
    [Description("Not Disclosed / Prefer Not to Say")]
    ND = 0,

    /// <summary>Single: unmarried, divorced, or legally separated</summary>
    [Description("Unmarried, divorced, or legally separated")]
    S = 1,

    /// <summary>Married, filing a joint return</summary>
    [Description("Married Filing Jointly")]
    MFJ = 2,

    /// <summary>Married, filing a separate return</summary>
    [Description("Married Filing Separately")]
    MFS = 3,

    /// <summary>Head of household</summary>
    [Description("Head of Household")]
    HOH = 4,

    /// <summary>Qualifying widow(er) with a dependent child</summary>
    [Description("Qualifying Widow(er)")]
    QW = 5
}