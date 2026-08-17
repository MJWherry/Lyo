namespace Lyo.Formatter;

/// <summary>Kind of a <see cref="FormatterSegment" /> produced by annotated formatting.</summary>
public enum FormatterSegmentKind
{
    /// <summary>Literal text copied from the template.</summary>
    Literal = 0,

    /// <summary>A placeholder that resolved to a replacement value.</summary>
    Placeholder = 1,

    /// <summary>A placeholder that was left in the output (missing context or <c>MaintainTokens</c>).</summary>
    Unresolved = 2
}
