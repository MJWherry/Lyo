using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>X12 / ANSI EDI — Element 1067: Marital Status Code</summary>
public enum MaritalStatus
{
    /// <summary>Unknown</summary>
    [Description("Unknown")]
    K = 0,

    /// <summary>Unreported</summary>
    [Description("Unreported")]
    R = 1,

    /// <summary>Not Applicable</summary>
    [Description("Not Applicable")]
    C = 2,

    /// <summary>Single</summary>
    [Description("Single")]
    I = 3,

    /// <summary>Unmarried (Single or Divorced or Widowed)</summary>
    [Description("Unmarried (Single or Divorced or Widowed)")]
    U = 4,

    /// <summary>Common Law</summary>
    [Description("Common Law")]
    A = 5,

    /// <summary>Registered Domestic Partner</summary>
    [Description("Registered Domestic Partner")]
    B = 6,

    /// <summary>Married</summary>
    [Description("Married")]
    M = 7,

    /// <summary>Divorced</summary>
    [Description("Divorced")]
    D = 8,

    /// <summary>Separated</summary>
    [Description("Separated")]
    S = 9,

    /// <summary>Legally Separated</summary>
    [Description("Legally Separated")]
    X = 10,

    /// <summary>Widowed</summary>
    [Description("Widowed")]
    W = 11
}