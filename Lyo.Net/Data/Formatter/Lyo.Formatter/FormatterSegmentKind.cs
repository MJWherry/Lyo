namespace Lyo.Formatter;

/// <summary>Role of a <see cref="FormatterSegment" /> from annotated formatting.</summary>
public enum FormatterSegmentKind
{
    /// <summary>Literal text taken from the template.</summary>
    Literal = 0,

    /// <summary>Placeholder that resolved to a replacement value.</summary>
    Placeholder = 1,

    /// <summary>Placeholder left in the output (missing context or <c>MaintainTokens</c>).</summary>
    Unresolved = 2
}
