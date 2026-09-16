namespace Lyo.Law.Models.Enums;

/// <summary>Official note attached to a statute node. Private comments stay on the Note plugin.</summary>
public enum LawAnnotationKind
{
    /// <summary>Enactment and amendment history.</summary>
    History,

    /// <summary>A citation to another provision.</summary>
    CrossReference,

    /// <summary>A compiler or codification note.</summary>
    CompilerNote,

    /// <summary>A reference inside the statutory text.</summary>
    ReferenceInText,

    /// <summary>A special provision, including an appendix note.</summary>
    SpecialProvision
}
