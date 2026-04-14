using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>OFCCP Disability Status compliance</summary>
public enum DisabilityStatus
{
    /// <summary>Unknown / Not provided</summary>
    [Description("Unknown / Not provided")]
    U = 0,

    /// <summary>Prefer not to say / Not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>No disability</summary>
    [Description("No disability")]
    N = 2,

    /// <summary>Disability</summary>
    [Description("Disability")]
    D = 3,

    /// <summary>Disability (self-identified)</summary>
    [Description("Disability (self-identified)")]
    PD = 4
}