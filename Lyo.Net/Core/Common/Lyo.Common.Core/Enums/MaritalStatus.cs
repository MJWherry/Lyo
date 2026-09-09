using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Marital status codes from X12 / ANSI EDI element 1067.</summary>
public enum MaritalStatus
{
    /// <summary>Not known</summary>
    [Description("Unknown")]
    K = 0,

    /// <summary>Not reported</summary>
    [Description("Unreported")]
    R = 1,

    /// <summary>Does not apply</summary>
    [Description("Not Applicable")]
    C = 2,

    /// <summary>Never married / single</summary>
    [Description("Single")]
    I = 3,

    /// <summary>Unmarried: single, divorced, or widowed</summary>
    [Description("Unmarried (Single or Divorced or Widowed)")]
    U = 4,

    /// <summary>Common-law marriage</summary>
    [Description("Common Law")]
    A = 5,

    /// <summary>Registered domestic partnership</summary>
    [Description("Registered Domestic Partner")]
    B = 6,

    /// <summary>Currently married</summary>
    [Description("Married")]
    M = 7,

    /// <summary>Marriage ended by divorce</summary>
    [Description("Divorced")]
    D = 8,

    /// <summary>Separated from spouse</summary>
    [Description("Separated")]
    S = 9,

    /// <summary>Legally separated from spouse</summary>
    [Description("Legally Separated")]
    X = 10,

    /// <summary>Spouse is deceased</summary>
    [Description("Widowed")]
    W = 11
}