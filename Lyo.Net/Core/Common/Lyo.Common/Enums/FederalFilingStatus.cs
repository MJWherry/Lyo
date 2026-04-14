using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Internal Revenue Service standard filing statuses</summary>
public enum FederalFilingStatus
{
    /// <summary>Not Disclosed / Prefer Not to Say</summary>
    [Description("Not Disclosed / Prefer Not to Say")]
    ND = 0,

    /// <summary>Unmarried, divorced, or legally separated</summary>
    [Description("Unmarried, divorced, or legally separated")]
    S = 1,

    /// <summary>Married Filing Jointly</summary>
    [Description("Married Filing Jointly")]
    MFJ = 2,

    /// <summary>Married Filing Separately</summary>
    [Description("Married Filing Separately")]
    MFS = 3,

    /// <summary>Head of Household</summary>
    [Description("Head of Household")]
    HOH = 4,

    /// <summary>Qualifying Widow(er) with Dependent Child/// </summary>
    [Description("Qualifying Widow(er)")]
    QW = 5
}