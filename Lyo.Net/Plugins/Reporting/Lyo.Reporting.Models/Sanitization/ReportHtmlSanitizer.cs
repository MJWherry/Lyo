using System.Net;
using System.Text;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Sanitization;

/// <summary>
/// Allowlist sanitizer for raw markup carried by <see cref="ContentType.Html" /> and <see cref="ContentType.Chart" /> content blocks. Composition JSON is
/// caller-supplied, so anything rendered as markup must pass through here first.
/// </summary>
/// <remarks>
/// This is a re-emitting sanitizer, not a blocklist: input is tokenized and only allowed elements and attributes are written to the output, so an element or attribute
/// nobody thought of is dropped rather than passed through. Raw-text elements (<c>script</c>, <c>style</c>, and friends) are dropped along with their content, event-handler
/// attributes are never emitted, and <c>href</c> / <c>src</c> values are restricted to safe schemes.
/// </remarks>
public static class ReportHtmlSanitizer
{
    private static readonly HashSet<string> AllowedElements = new(StringComparer.OrdinalIgnoreCase) {
        "a", "abbr", "article", "aside", "b", "blockquote", "br", "canvas", "caption", "code", "col", "colgroup", "dd", "div", "dl", "dt", "em", "figcaption", "figure",
        "footer", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "i", "img", "label", "li", "main", "mark", "ol", "p", "pre", "s", "section", "small", "span", "strong",
        "sub", "sup", "table", "tbody", "td", "tfoot", "th", "thead", "time", "tr", "u", "ul"
    };

    /// <summary>Elements dropped together with everything up to their closing tag, because their content is script, styling, or an embedded document rather than markup.</summary>
    private static readonly HashSet<string> DroppedWithContent = new(StringComparer.OrdinalIgnoreCase) {
        "script", "style", "iframe", "frame", "frameset", "object", "embed", "applet", "noscript", "noembed", "svg", "math", "template", "textarea", "title", "xmp"
    };

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) { "br", "col", "hr", "img" };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase) {
        "align", "alt", "class", "colspan", "dir", "headers", "height", "href", "id", "lang", "rel", "rowspan", "scope", "span", "src", "style", "target", "title", "valign", "width"
    };

    /// <summary>Attributes whose value is a URL, so they need a scheme check rather than only escaping.</summary>
    private static readonly HashSet<string> UrlAttributes = new(StringComparer.OrdinalIgnoreCase) { "href", "src" };

    private static readonly string[] AllowedUrlSchemes = ["http:", "https:", "mailto:", "tel:"];

    private static readonly string[] AllowedDataUrlPrefixes = ["data:image/png;", "data:image/jpeg;", "data:image/gif;", "data:image/webp;"];

    /// <summary>
    /// Sanitizes a CSS property value (layout margin/padding, inline style dictionaries). Drops declarations that can execute.
    /// Null or empty input returns <see cref="string.Empty" />.
    /// </summary>
    public static string SanitizeCss(string? value) => string.IsNullOrEmpty(value) ? string.Empty : SanitizeStyle(value!);

    /// <summary>
    /// Returns <paramref name="html" /> with every element and attribute outside the allowlist removed. Null or empty input returns <see cref="string.Empty" />, so callers
    /// can pass the value straight to a markup sink.
    /// </summary>
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var output = new StringBuilder(html!.Length);
        var index = 0;
        while (index < html.Length) {
            var next = html.IndexOf('<', index);
            if (next < 0) {
                AppendText(output, html, index, html.Length);
                break;
            }

            AppendText(output, html, index, next);
            index = WriteTag(output, html, next);
        }

        return output.ToString();
    }

    /// <summary>
    /// Handles the tag (or comment, or stray <c>&lt;</c>) starting at <paramref name="start" /> and returns the index to keep scanning from. Emits the tag only when it
    /// survives the allowlist.
    /// </summary>
    private static int WriteTag(StringBuilder output, string html, int start)
    {
        if (start + 1 >= html.Length) {
            output.Append("&lt;");
            return html.Length;
        }

        // Comments, doctypes, and CDATA carry no report content and can hide markup from naive parsers, so they are dropped whole. A comment ends at "-->", not at the first
        // '>', which may sit inside the commented-out markup.
        if (html.Length > start + 3 && html[start + 1] == '!' && html[start + 2] == '-' && html[start + 3] == '-') {
            var commentEnd = html.IndexOf("-->", start + 4, StringComparison.Ordinal);
            return commentEnd < 0 ? html.Length : commentEnd + 3;
        }

        if (html[start + 1] == '!' || html[start + 1] == '?') {
            var declarationEnd = html.IndexOf('>', start);
            return declarationEnd < 0 ? html.Length : declarationEnd + 1;
        }

        var closing = html[start + 1] == '/';
        var nameStart = start + (closing ? 2 : 1);
        var nameEnd = nameStart;
        while (nameEnd < html.Length && (char.IsLetterOrDigit(html[nameEnd]) || html[nameEnd] == '-'))
            nameEnd++;

        if (nameEnd == nameStart) {
            // Not a tag at all (for example "a < b"): escape the bracket so it cannot combine with later input into an element.
            output.Append("&lt;");
            return start + 1;
        }

        var name = html.Substring(nameStart, nameEnd - nameStart);
        var tagEnd = FindTagEnd(html, nameEnd);
        if (DroppedWithContent.Contains(name))
            return closing ? tagEnd : SkipElementContent(html, name, tagEnd);

        if (!AllowedElements.Contains(name))
            return tagEnd;

        if (closing) {
            if (!VoidElements.Contains(name))
                output.Append("</").Append(name.ToLowerInvariant()).Append('>');

            return tagEnd;
        }

        WriteOpenTag(output, name, html, nameEnd, tagEnd);
        return tagEnd;
    }

    /// <summary>Index just past the <c>&gt;</c> that closes the tag whose name ended at <paramref name="from" />, honoring quoted attribute values.</summary>
    private static int FindTagEnd(string html, int from)
    {
        var quote = '\0';
        for (var i = from; i < html.Length; i++) {
            var c = html[i];
            if (quote != '\0') {
                if (c == quote)
                    quote = '\0';

                continue;
            }

            if (c is '"' or '\'')
                quote = c;
            else if (c == '>')
                return i + 1;
        }

        return html.Length;
    }

    /// <summary>Skips to just past the closing tag of a dropped raw-text element, so that content never reaches the output.</summary>
    private static int SkipElementContent(string html, string name, int contentStart)
    {
        var closeTag = "</" + name;
        var close = html.IndexOf(closeTag, contentStart, StringComparison.OrdinalIgnoreCase);
        if (close < 0)
            return html.Length;

        var end = html.IndexOf('>', close);
        return end < 0 ? html.Length : end + 1;
    }

    private static void WriteOpenTag(StringBuilder output, string name, string html, int attributesStart, int tagEnd)
    {
        var lower = name.ToLowerInvariant();
        output.Append('<').Append(lower);
        var hasTarget = false;
        foreach (var (attribute, value) in ReadAttributes(html, attributesStart, tagEnd)) {
            if (!IsAllowedAttribute(attribute))
                continue;

            var effective = value;
            if (UrlAttributes.Contains(attribute)) {
                if (!IsAllowedUrl(value))
                    continue;
            }
            else if (attribute.Equals("style", StringComparison.OrdinalIgnoreCase)) {
                effective = SanitizeStyle(value);
                if (effective.Length == 0)
                    continue;
            }
            else if (attribute.Equals("target", StringComparison.OrdinalIgnoreCase))
                hasTarget = true;

            output.Append(' ').Append(attribute.ToLowerInvariant()).Append("=\"").Append(WebUtility.HtmlEncode(effective)).Append('"');
        }

        // A surviving target="_blank" gets opener isolation; the source markup is not trusted to have set that attribute.
        if (hasTarget && lower == "a")
            output.Append(" rel=\"noopener noreferrer\"");

        if (VoidElements.Contains(lower))
            output.Append(" /");

        output.Append('>');
    }

    /// <summary>Parses <c>name</c> / <c>name=value</c> pairs from the attribute region of a tag. Values may be double-quoted, single-quoted, or bare.</summary>
    private static IEnumerable<(string Name, string Value)> ReadAttributes(string html, int start, int tagEnd)
    {
        var i = start;
        var limit = tagEnd > start && html[tagEnd - 1] == '>' ? tagEnd - 1 : tagEnd;
        while (i < limit) {
            while (i < limit && (char.IsWhiteSpace(html[i]) || html[i] == '/'))
                i++;

            var nameStart = i;
            while (i < limit && !char.IsWhiteSpace(html[i]) && html[i] != '=' && html[i] != '/')
                i++;

            if (i == nameStart)
                break;

            var name = html.Substring(nameStart, i - nameStart);
            while (i < limit && char.IsWhiteSpace(html[i]))
                i++;

            if (i >= limit || html[i] != '=') {
                yield return (name, string.Empty);
                continue;
            }

            i++;
            while (i < limit && char.IsWhiteSpace(html[i]))
                i++;

            if (i >= limit) {
                yield return (name, string.Empty);
                break;
            }

            string value;
            var quote = html[i];
            if (quote is '"' or '\'') {
                i++;
                var valueStart = i;
                while (i < limit && html[i] != quote)
                    i++;

                value = html.Substring(valueStart, i - valueStart);
                if (i < limit)
                    i++;
            }
            else {
                var valueStart = i;
                while (i < limit && !char.IsWhiteSpace(html[i]))
                    i++;

                value = html.Substring(valueStart, i - valueStart);
            }

            yield return (name, WebUtility.HtmlDecode(value));
        }
    }

    /// <summary>
    /// Data attributes are inert markup, so they are allowed wholesale. That is what carries chart configuration to the viewer's own bootstrap script. Event handlers
    /// (<c>on*</c>) and everything else outside <see cref="AllowedAttributes" /> are dropped.
    /// </summary>
    private static bool IsAllowedAttribute(string attribute)
    {
        if (attribute.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            return false;

        return attribute.StartsWith("data-", StringComparison.OrdinalIgnoreCase) || AllowedAttributes.Contains(attribute);
    }

    /// <summary>Accepts relative URLs and a small set of schemes. Anything else (<c>javascript:</c>, <c>vbscript:</c>, or non-image <c>data:</c>) is rejected.</summary>
    private static bool IsAllowedUrl(string value)
    {
        var trimmed = StripControlCharacters(value).TrimStart();
        if (trimmed.Length == 0)
            return false;

        foreach (var scheme in AllowedUrlSchemes) {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var prefix in AllowedDataUrlPrefixes) {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Relative URLs have no scheme; a colon before the first '/', '?' or '#' means there is one.
        var colon = trimmed.IndexOf(':');
        if (colon < 0)
            return true;

        var separator = trimmed.IndexOfAny(['/', '?', '#']);
        return separator >= 0 && separator < colon;
    }

    /// <summary>Drops declarations that can execute (<c>expression()</c>, <c>url(javascript:)</c>, <c>behavior</c>) and keeps the rest of the inline style.</summary>
    private static string SanitizeStyle(string value)
    {
        var cleaned = StripControlCharacters(value);
        var kept = new List<string>();
        foreach (var declaration in cleaned.Split(';')) {
            var trimmed = declaration.Trim();
            if (trimmed.Length == 0)
                continue;

            var lower = trimmed.ToLowerInvariant();
            if (lower.Contains("expression(") || lower.Contains("javascript:") || lower.Contains("vbscript:") || lower.StartsWith("behavior", StringComparison.Ordinal) ||
                lower.StartsWith("-moz-binding", StringComparison.Ordinal))
                continue;

            kept.Add(trimmed);
        }

        return kept.Count == 0 ? string.Empty : string.Join("; ", kept);
    }

    /// <summary>Removes NUL, newlines, and other control characters used to break up a blocked token (for example <c>java\0script:</c>).</summary>
    private static string StripControlCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value) {
            if (!char.IsControl(c))
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>Copies text between tags. Bare <c>&lt;</c> is handled by the caller, so only <c>&gt;</c> is neutralized here.</summary>
    private static void AppendText(StringBuilder output, string html, int start, int end)
    {
        for (var i = start; i < end; i++) {
            var c = html[i];
            if (c == '>')
                output.Append("&gt;");
            else
                output.Append(c);
        }
    }
}
