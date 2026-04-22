using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Shared single-attempt element wait used by <see cref="SeleniumBrowser.WaitFor" /> and <see cref="SeleniumBrowser.GetElementAsync(OpenQA.Selenium.By,System.Threading.CancellationToken)" /> (and chained resolution in <see cref="SeleniumBrowser" />).</summary>
internal static class SeleniumPolling
{
    /// <summary>One bounded <see cref="WebDriverWait" /> for <paramref name="by" />.</summary>
    public static IWebElement? TryWaitForElement(
        IWebDriver driver,
        By by,
        int seleniumMaxWaitSeconds,
        ILogger? logger,
        CancellationToken ct)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seleniumMaxWaitSeconds));
        try {
            return wait.Until(d => {
                ct.ThrowIfCancellationRequested();
                return d.FindElement(by);
            });
        }
        catch (WebDriverException ex) when (ex is NoSuchElementException or WebDriverTimeoutException) {
            logger?.LogDebug(ex, "Element not found: {Locator}", by);
            return null;
        }
        catch (OperationCanceledException) {
            return null;
        }
    }

    /// <summary>Waits for <paramref name="by" /> relative to <paramref name="context" /> (nested search).</summary>
    public static IWebElement? TryWaitForNestedElement(
        IWebDriver driver,
        IWebElement context,
        By by,
        int seleniumMaxWaitSeconds,
        ILogger? logger,
        CancellationToken ct)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seleniumMaxWaitSeconds));
        try {
            return wait.Until(_ => {
                ct.ThrowIfCancellationRequested();
                try {
                    return context.FindElement(by);
                }
                catch (NoSuchElementException) {
                    return null;
                }
                catch (StaleElementReferenceException) {
                    return null;
                }
            });
        }
        catch (WebDriverException ex) when (ex is WebDriverTimeoutException) {
            logger?.LogDebug(ex, "Nested element not found: {Locator}", by);
            return null;
        }
        catch (OperationCanceledException) {
            return null;
        }
    }
}
