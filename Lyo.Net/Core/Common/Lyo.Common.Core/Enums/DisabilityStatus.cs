using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>OFCCP disability-status categories used for compliance reporting.</summary>
public enum DisabilityStatus
{
    /// <summary>Not known or not supplied</summary>
    [Description("Unknown / Not provided")]
    U = 0,

    /// <summary>Declined to answer / not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>Reports no disability</summary>
    [Description("No disability")]
    N = 2,

    /// <summary>Reports a disability</summary>
    [Description("Disability")]
    D = 3,

    /// <summary>Self-identified disability</summary>
    [Description("Disability (self-identified)")]
    PD = 4
}