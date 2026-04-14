using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>OMB Statistical Policy Directive 15 — Hispanic or Latino ethnicity (separate from race for EEO reporting).</summary>
public enum Ethnicity
{
    /// <summary>Unknown / Not Reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Prefer not to say / Not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>Hispanic or Latino</summary>
    [Description("Hispanic or Latino")]
    H = 2,

    /// <summary>Not Hispanic or Latino</summary>
    [Description("Not Hispanic or Latino")]
    N = 3
}