using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum Sex
{
    /// <summary>Unknown</summary>
    [Description("Unknown")]
    U = 0,

    /// <summary>Male</summary>
    [Description("Male")]
    M = 1,

    /// <summary>Female</summary>
    [Description("Female")]
    F = 2
}