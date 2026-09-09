using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum Sex
{
    /// <summary>Not known</summary>
    [Description("Unknown")]
    U = 0,

    /// <summary>Male sex</summary>
    [Description("Male")]
    M = 1,

    /// <summary>Female sex</summary>
    [Description("Female")]
    F = 2
}