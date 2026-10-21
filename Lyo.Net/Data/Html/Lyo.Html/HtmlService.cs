using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Lyo.Exceptions;

namespace Lyo.Html;

/// <summary>AngleSharp HTML reader and allowlist sanitizer.</summary>
public sealed class HtmlService : IHtmlService
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) {
        "a",
        "p",
        "br",
        "strong",
        "em",
        "ul",
        "ol",
        "li"
    };

    private readonly HtmlParser _parser = new();

    /// <inheritdoc />
    public string VisibleText(string html)
    {
        ArgumentHelpers.ThrowIfNull(html);
        var document = _parser.ParseDocument(html);
        foreach (var hidden in document.QuerySelectorAll("script, style").ToList())
            hidden.Remove();

        return document.Body?.TextContent ?? string.Empty;
    }

    /// <inheritdoc />
    public string Sanitize(string html)
    {
        ArgumentHelpers.ThrowIfNull(html);
        var document = _parser.ParseDocument(html);
        var body = document.Body;
        if (body is null)
            return string.Empty;

        Clean(body);
        return body.InnerHtml;
    }

    private static void Clean(IElement element)
    {
        foreach (var child in element.Children.ToList()) {
            if (child.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase) || child.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase)) {
                child.Remove();
                continue;
            }

            Clean(child);
            if (!Allowed.Contains(child.LocalName)) {
                while (child.FirstChild is not null)
                    element.InsertBefore(child.FirstChild, child);

                child.Remove();
                continue;
            }

            StripAttributes(child);
        }
    }

    private static void StripAttributes(IElement element)
    {
        foreach (var attribute in element.Attributes.ToList()) {
            if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)) {
                element.RemoveAttribute(attribute.Name);
                continue;
            }

            if (element.LocalName.Equals("a", StringComparison.OrdinalIgnoreCase) && attribute.Name.Equals("href", StringComparison.OrdinalIgnoreCase) &&
                IsSafeHref(attribute.Value))
                continue;

            element.RemoveAttribute(attribute.Name);
        }
    }

    private static bool IsSafeHref(string href)
    {
        var trimmed = href.Trim();
        return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
    }
}