using System.ComponentModel;

namespace Lyo.Common.Enums;

public enum NamePrefix
{
    /// <summary>Unknown or unspecified prefix</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Mr. — Male honorific</summary>
    [Description("Mr.")]
    Mr = 1,

    /// <summary>Mrs. — Married female honorific</summary>
    [Description("Mrs.")]
    Mrs = 2,

    /// <summary>Ms. — Female honorific (marital status unspecified)</summary>
    [Description("Ms.")]
    Ms = 3,

    /// <summary>Miss — Unmarried female honorific</summary>
    [Description("Miss")]
    Miss = 4,

    /// <summary>Dr. — Doctor (academic or medical title)</summary>
    [Description("Dr.")]
    Dr = 5,

    /// <summary>Prof. — Professor (academic title)</summary>
    [Description("Prof.")]
    Prof = 6,

    /// <summary>Rev. — Reverend (religious title)</summary>
    [Description("Rev.")]
    Rev = 7,

    /// <summary>Hon. — Honorable (used for judges, officials, etc.)</summary>
    [Description("Hon.")]
    Hon = 8,

    /// <summary>Sir — Male knighted or honorific title</summary>
    [Description("Sir")]
    Sir = 9,

    /// <summary>Madam — Formal female honorific (e.g., “Madam Chair”)</summary>
    [Description("Madam")]
    Madam = 10,

    /// <summary>Mx. — Gender-neutral honorific</summary>
    [Description("Mx.")]
    Mx = 11
}