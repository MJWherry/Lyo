using Lyo.Exceptions;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Lightweight page/component base: holds <see cref="SeleniumBrowser" /> and an optional root <see cref="By" /> for nested locators.</summary>
public abstract class PageBase
{
    protected PageBase(SeleniumBrowser browser, By? root = null)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        Browser = browser;
        Root = root;
    }

    /// <summary>Browser API for this page.</summary>
    public SeleniumBrowser Browser { get; }

    /// <summary>Optional container locator (e.g. shadow host or widget root).</summary>
    public By? Root { get; }
}
