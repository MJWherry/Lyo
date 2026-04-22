using Lyo.Exceptions;
using Lyo.Web.Automation.Models.Enums;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Automation;

internal static class ElementLocatorMapping
{
    public static By ToBy(ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(locator.Value, nameof(locator.Value));
        return locator.Kind switch {
            ElementLocatorKind.Id => By.Id(locator.Value),
            ElementLocatorKind.Name => By.Name(locator.Value),
            ElementLocatorKind.CssSelector => By.CssSelector(locator.Value),
            ElementLocatorKind.XPath => By.XPath(locator.Value),
            ElementLocatorKind.LinkText => By.LinkText(locator.Value),
            ElementLocatorKind.PartialLinkText => By.PartialLinkText(locator.Value),
            ElementLocatorKind.ClassName => By.ClassName(locator.Value),
            ElementLocatorKind.TagName => By.TagName(locator.Value),
            var _ => throw new ArgumentOutOfRangeException(nameof(locator), locator.Kind, "Unknown locator kind.")
        };
    }
}