using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum PhoneType
{
    /// <summary>Unknown</summary>
    [Description("Unknown")]
    U = 0,

    /// <summary>Landline</summary>
    [Description("Landline")]
    L = 1,

    /// <summary>Mobile</summary>
    [Description("Mobile")]
    M = 2,

    /// <summary>Voice over IP</summary>
    [Description("Voice over IP")]
    V = 3
}