using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Demographic categories for religion or religious affiliation.</summary>
public enum Religion
{
    /// <summary>Not known or not reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Declined to answer / not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>No religious affiliation</summary>
    [Description("No religion")]
    N = 2,

    /// <summary>Christian affiliation</summary>
    [Description("Christian")]
    C = 3,

    /// <summary>Jewish affiliation</summary>
    [Description("Jewish")]
    J = 4,

    /// <summary>Muslim affiliation</summary>
    [Description("Muslim")]
    M = 5,

    /// <summary>Buddhist affiliation</summary>
    [Description("Buddhist")]
    B = 6,

    /// <summary>Hindu affiliation</summary>
    [Description("Hindu")]
    H = 7,

    /// <summary>Sikh affiliation</summary>
    [Description("Sikh")]
    S = 8,

    /// <summary>Atheist (no belief in deity)</summary>
    [Description("Atheist")]
    A = 9,

    /// <summary>Another religion not listed</summary>
    [Description("Other")]
    O = 10
}