using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum YesNo
{
    /// <summary>No</summary>
    [Description("No")]
    N = 0,

    /// <summary>Yes</summary>
    [Description("Yes")]
    Y = 1,

    /// <summary>Unknown</summary>
    [Description("Unknown")]
    U = 2
}