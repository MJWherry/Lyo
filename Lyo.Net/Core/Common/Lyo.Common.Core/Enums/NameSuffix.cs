using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

// ReSharper disable InconsistentNaming
public enum NameSuffix
{
    /// <summary>No suffix, or none recorded</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Junior (Jr.)</summary>
    [Description("Jr.")]
    Jr = 1,

    /// <summary>Senior (Sr.)</summary>
    [Description("Sr.")]
    Sr = 2,

    /// <summary>The second (II)</summary>
    [Description("II")]
    II = 3,

    /// <summary>The third (III)</summary>
    [Description("III")]
    III = 4,

    /// <summary>The fourth (IV)</summary>
    [Description("IV")]
    IV = 5,

    /// <summary>Esquire (Esq.)</summary>
    [Description("Esq.")]
    Esq = 6,

    /// <summary>Doctor of Philosophy (PhD)</summary>
    [Description("PhD")]
    PhD = 7,

    /// <summary>Doctor of Medicine (MD)</summary>
    [Description("MD")]
    MD = 8,

    /// <summary>Doctor of Dental Surgery (DDS)</summary>
    [Description("DDS")]
    DDS = 9,

    /// <summary>Doctor of Veterinary Medicine (DVM)</summary>
    [Description("DVM")]
    DVM = 10,

    /// <summary>Certified Public Accountant (CPA)</summary>
    [Description("CPA")]
    CPA = 11
}