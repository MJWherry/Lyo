using Lyo.Exceptions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.PuppeteerSharp;

/// <summary>Maps <see cref="ElementLocator" /> onto a Puppeteer selector string.</summary>
internal static class PuppeteerSharpLocatorFactory
{
    public static string ToSelector(ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(locator);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => $"[id=\"{CssEscape(locator.Value)}\"]",
            ElementLocatorKind.Name => $"[name=\"{CssEscape(locator.Value)}\"]",
            ElementLocatorKind.CssSelector => locator.Value,
            ElementLocatorKind.XPath => $"xpath/{locator.Value}",
            ElementLocatorKind.LinkText => $"xpath//a[normalize-space(.)={XPathLiteral(locator.Value)}]",
            ElementLocatorKind.PartialLinkText => $"xpath//a[contains(normalize-space(.),{XPathLiteral(locator.Value)})]",
            ElementLocatorKind.ClassName => "." + string.Join(".", locator.Value.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(CssEscapeClass)),
            ElementLocatorKind.TagName => locator.Value,
            var _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, null)
        };
    }

    private static string CssEscape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string CssEscapeClass(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string XPathLiteral(string value)
    {
        if (!value.Contains('\''))
            return $"'{value}'";

        if (!value.Contains('"'))
            return $"\"{value}\"";

        var parts = value.Split('\'');
        return "concat('" + string.Join("', \"'\", '", parts) + "')";
    }
}
