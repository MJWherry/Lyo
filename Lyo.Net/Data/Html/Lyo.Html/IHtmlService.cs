namespace Lyo.Html;

/// <summary>Parses an HTML document, reads visible text, and sanitizes markup with an allowlist.</summary>
public interface IHtmlService
{
    /// <summary>Text a reader would see. Contents of <c>script</c> and <c>style</c> are omitted.</summary>
    string VisibleText(string html);

    /// <summary>
    /// Keeps allowlisted elements (<c>a</c>, <c>p</c>, <c>br</c>, <c>strong</c>, <c>em</c>, <c>ul</c>, <c>ol</c>, <c>li</c>) and safe <c>href</c> values. Drops <c>script</c>,
    /// event-handler attributes, and <c>javascript:</c> URLs. Other elements are unwrapped so their text remains.
    /// </summary>
    string Sanitize(string html);
}