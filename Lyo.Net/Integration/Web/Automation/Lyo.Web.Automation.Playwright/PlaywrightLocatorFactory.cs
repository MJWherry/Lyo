using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Web.Automation.Enums;
using Lyo.Web.Automation.Models;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright;

internal static class PlaywrightLocatorFactory
{
    public static ILocator Locate(IPage page, ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(page, nameof(page));
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => page.Locator(CssIdSelector(locator.Value)),
            ElementLocatorKind.Name => page.Locator($"[name=\"{CssEscape(locator.Value)}\"]"),
            ElementLocatorKind.CssSelector => page.Locator(locator.Value),
            ElementLocatorKind.XPath => page.Locator($"xpath={locator.Value}"),
            ElementLocatorKind.LinkText => page.GetByText(locator.Value, new() { Exact = true }),
            ElementLocatorKind.PartialLinkText => page.GetByText(new Regex(Regex.Escape(locator.Value))),
            ElementLocatorKind.ClassName => page.Locator($".{CssEscapeClass(locator.Value)}"),
            ElementLocatorKind.TagName => page.Locator(locator.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, null)
        };
    }

    public static ILocator Locate(IFrameLocator frame, ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(frame, nameof(frame));
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => frame.Locator(CssIdSelector(locator.Value)),
            ElementLocatorKind.Name => frame.Locator($"[name=\"{CssEscape(locator.Value)}\"]"),
            ElementLocatorKind.CssSelector => frame.Locator(locator.Value),
            ElementLocatorKind.XPath => frame.Locator($"xpath={locator.Value}"),
            ElementLocatorKind.LinkText => frame.GetByText(locator.Value, new() { Exact = true }),
            ElementLocatorKind.PartialLinkText => frame.GetByText(new Regex(Regex.Escape(locator.Value))),
            ElementLocatorKind.ClassName => frame.Locator($".{CssEscapeClass(locator.Value)}"),
            ElementLocatorKind.TagName => frame.Locator(locator.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, null)
        };
    }

    /// <summary>Chained search under an existing <see cref="ILocator" /> (descendant scope).</summary>
    public static ILocator Locate(ILocator root, ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(root, nameof(root));
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => root.Locator(CssIdSelector(locator.Value)),
            ElementLocatorKind.Name => root.Locator($"[name=\"{CssEscape(locator.Value)}\"]"),
            ElementLocatorKind.CssSelector => root.Locator(locator.Value),
            ElementLocatorKind.XPath => root.Locator($"xpath={locator.Value}"),
            ElementLocatorKind.LinkText => root.GetByText(locator.Value, new() { Exact = true }),
            ElementLocatorKind.PartialLinkText => root.GetByText(new Regex(Regex.Escape(locator.Value))),
            ElementLocatorKind.ClassName => root.Locator($".{CssEscapeClass(locator.Value)}"),
            ElementLocatorKind.TagName => root.Locator(locator.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, null)
        };
    }

    private static string CssIdSelector(string id)
        => $"[id=\"{CssEscape(id)}\"]";

    private static string CssEscape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string CssEscapeClass(string value)
        => string.Join(".", value.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(CssEscape));
}
