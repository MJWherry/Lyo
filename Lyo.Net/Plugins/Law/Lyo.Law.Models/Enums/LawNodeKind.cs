namespace Lyo.Law.Models.Enums;

/// <summary>Outline level of a state statute node. States use different subsets of these levels.</summary>
public enum LawNodeKind
{
    /// <summary>A title, such as Pennsylvania Title 67.</summary>
    Title,

    /// <summary>A division. Pennsylvania palegis <c>div=0</c> means the title has no division node.</summary>
    Division,

    /// <summary>A part under a title or code.</summary>
    Part,

    /// <summary>An article under a part or chapter.</summary>
    Article,

    /// <summary>A chapter.</summary>
    Chapter,

    /// <summary>A subchapter.</summary>
    Subchapter,

    /// <summary>A citable section.</summary>
    Section,

    /// <summary>A subsection, such as <c>(a)</c>.</summary>
    Subsection,

    /// <summary>A paragraph, such as <c>(1)</c>.</summary>
    Paragraph,

    /// <summary>A subparagraph, such as <c>(i)</c>.</summary>
    Subparagraph,

    /// <summary>A clause.</summary>
    Clause,

    /// <summary>A subclause.</summary>
    Subclause
}
