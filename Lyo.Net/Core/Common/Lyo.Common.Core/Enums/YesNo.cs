using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum YesNo
{
    /// <summary>Negative / no</summary>
    [Description("No")]
    N = 0,

    /// <summary>Affirmative / yes</summary>
    [Description("Yes")]
    Y = 1,

    /// <summary>Not known</summary>
    [Description("Unknown")]
    U = 2
}