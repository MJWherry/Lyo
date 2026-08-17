namespace Lyo.Formatter;

/// <summary>
/// One span of a template after annotated formatting. Literals keep their source text; placeholders carry the replacement (or the raw token when unresolved) plus the
/// placeholder key used to color-link editor and preview UIs.
/// </summary>
/// <param name="Kind">Whether this span is literal text, a resolved replacement, or an unresolved token.</param>
/// <param name="Text">Display text: unescaped literal, replacement value, or the raw <c>{Name}</c> token when unresolved.</param>
/// <param name="PlaceholderKey">Placeholder path (e.g. <c>Name</c>, <c>Docket.Number</c>). Null for literals.</param>
/// <param name="RawToken">Exact substring from the template (escaped literals and <c>{Name:N0}</c> tokens). Used to rebuild a highlighted template overlay.</param>
public sealed record FormatterSegment(FormatterSegmentKind Kind, string Text, string? PlaceholderKey, string? RawToken);
