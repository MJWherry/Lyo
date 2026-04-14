using System.ComponentModel;

namespace Lyo.Common.Enums;

// ReSharper disable InconsistentNaming
public enum NameSuffix
{
    /// <summary>Unknown</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Jr.</summary>
    [Description("Jr.")]
    Jr = 1,

    /// <summary>Sr.</summary>
    [Description("Sr.")]
    Sr = 2,

    /// <summary>II</summary>
    [Description("II")]
    II = 3,

    /// <summary>III</summary>
    [Description("III")]
    III = 4,

    /// <summary>IV</summary>
    [Description("IV")]
    IV = 5,

    /// <summary>Esq.</summary>
    [Description("Esq.")]
    Esq = 6,

    /// <summary>PhD</summary>
    [Description("PhD")]
    PhD = 7,

    /// <summary>MD</summary>
    [Description("MD")]
    MD = 8,

    /// <summary>DDS</summary>
    [Description("DDS")]
    DDS = 9,

    /// <summary>DVM</summary>
    [Description("DVM")]
    DVM = 10,

    /// <summary>CPA</summary>
    [Description("CPA")]
    CPA = 11
}