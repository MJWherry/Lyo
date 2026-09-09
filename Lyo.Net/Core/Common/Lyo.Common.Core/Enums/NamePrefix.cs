using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum NamePrefix
{
    /// <summary>No prefix, or none recorded</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Mr. — honorific for a man</summary>
    [Description("Mr.")]
    Mr = 1,

    /// <summary>Mrs. — honorific for a married woman</summary>
    [Description("Mrs.")]
    Mrs = 2,

    /// <summary>Ms. — honorific for a woman (marital status not implied)</summary>
    [Description("Ms.")]
    Ms = 3,

    /// <summary>Miss — honorific for an unmarried woman</summary>
    [Description("Miss")]
    Miss = 4,

    /// <summary>Dr. — doctoral title (academic or medical)</summary>
    [Description("Dr.")]
    Dr = 5,

    /// <summary>Prof. — academic professor title</summary>
    [Description("Prof.")]
    Prof = 6,

    /// <summary>Rev. — religious title (reverend)</summary>
    [Description("Rev.")]
    Rev = 7,

    /// <summary>Hon. — used for judges, officials, and similar roles</summary>
    [Description("Hon.")]
    Hon = 8,

    /// <summary>Sir — knighthood or male honorific</summary>
    [Description("Sir")]
    Sir = 9,

    /// <summary>Madam — formal female honorific (for example “Madam Chair”)</summary>
    [Description("Madam")]
    Madam = 10,

    /// <summary>Mx. — gender-neutral honorific</summary>
    [Description("Mx.")]
    Mx = 11
}