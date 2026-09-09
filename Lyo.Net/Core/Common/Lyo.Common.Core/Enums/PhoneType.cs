using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum PhoneType
{
    /// <summary>Not known</summary>
    [Description("Unknown")]
    U = 0,

    /// <summary>Wired / landline telephone</summary>
    [Description("Landline")]
    L = 1,

    /// <summary>Cellular / mobile telephone</summary>
    [Description("Mobile")]
    M = 2,

    /// <summary>Voice-over-IP telephone</summary>
    [Description("Voice over IP")]
    V = 3
}