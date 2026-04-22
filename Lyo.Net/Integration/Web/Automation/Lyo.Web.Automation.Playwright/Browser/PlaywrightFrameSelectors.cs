using Lyo.Exceptions;
using Lyo.Web.Automation;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Maps <see cref="ElementLocator" /> to a CSS selector for an <c>iframe</c> element (for <see cref="Microsoft.Playwright.IPage.FrameLocator" />).</summary>
public static class PlaywrightFrameSelectors
{
    /// <summary>Builds a selector string for the iframe element that hosts the frame.</summary>
    public static string ToIframeSelector(ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => $"iframe#{CssEscape(locator.Value)}",
            ElementLocatorKind.Name => $"iframe[name=\"{CssEscape(locator.Value)}\"]",
            ElementLocatorKind.CssSelector => locator.Value.StartsWith("iframe", StringComparison.OrdinalIgnoreCase)
                ? locator.Value
                : $"iframe {locator.Value}",
            ElementLocatorKind.ClassName => $"iframe.{string.Join(".", locator.Value.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(CssEscapeClass))}",
            ElementLocatorKind.TagName => locator.Value.Equals("iframe", StringComparison.OrdinalIgnoreCase)
                ? "iframe"
                : $"iframe {locator.Value}",
            ElementLocatorKind.XPath => throw new NotSupportedException("XPath frame selection is not supported; use CssSelector or Id."),
            ElementLocatorKind.LinkText or ElementLocatorKind.PartialLinkText => throw new NotSupportedException("Link text frame selection is not supported for iframe targeting."),
            _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, null)
        };
    }

    private static string CssEscape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string CssEscapeClass(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
