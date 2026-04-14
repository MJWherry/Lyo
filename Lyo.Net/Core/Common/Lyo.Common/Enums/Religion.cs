using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Religion / religious affiliation categories for demographic data.</summary>
public enum Religion
{
    /// <summary>Unknown / Not Reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Prefer not to say / Not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>No religion</summary>
    [Description("No religion")]
    N = 2,

    /// <summary>Christian</summary>
    [Description("Christian")]
    C = 3,

    /// <summary>Jewish</summary>
    [Description("Jewish")]
    J = 4,

    /// <summary>Muslim</summary>
    [Description("Muslim")]
    M = 5,

    /// <summary>Buddhist</summary>
    [Description("Buddhist")]
    B = 6,

    /// <summary>Hindu</summary>
    [Description("Hindu")]
    H = 7,

    /// <summary>Sikh</summary>
    [Description("Sikh")]
    S = 8,

    /// <summary>Atheist</summary>
    [Description("Atheist")]
    A = 9,

    /// <summary>Other</summary>
    [Description("Other")]
    O = 10
}