using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Hispanic or Latino ethnicity per OMB Statistical Policy Directive 15 (tracked apart from race for EEO).</summary>
public enum Ethnicity
{
    /// <summary>Not known or not reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Declined to answer / not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>Identifies as Hispanic or Latino</summary>
    [Description("Hispanic or Latino")]
    H = 2,

    /// <summary>Does not identify as Hispanic or Latino</summary>
    [Description("Not Hispanic or Latino")]
    N = 3
}