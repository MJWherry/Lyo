using System.Diagnostics;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class LocatorTimeoutTests
{
    [Fact]
    public async Task PollForElement_LocatorTimeout_FailsOnTheFirstAttempt()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", "locator-timeout");
        Directory.CreateDirectory(root);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddPlaywrightBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = root;
            options.EnableMetrics = false;
            options.BrowserKind = AutomationBrowserKind.Chromium;
            options.LocatorDefaultTimeoutMs = 200;
            options.PollingMaxAttempts = 5;
            options.PollingDelayBetweenAttempts = TimeSpan.FromMilliseconds(500);
        });

        await using var provider = services.BuildServiceProvider();
        await using var session = provider.GetRequiredService<IPlaywrightBrowserService>().CreateSession();
        await session.StartBrowserAsync(TestContext.Current.CancellationToken);
        await session.Browser.NavigateToAsync("data:text/html,<html><body></body></html>", TestContext.Current.CancellationToken);
        var missing = new ElementLocatorChain(new ElementLocator(ElementLocatorKind.CssSelector, "#does-not-exist"));
        var watch = Stopwatch.StartNew();
        Exception? error = null;
        try {
            await session.Browser.PollForChainAsync(missing, TestContext.Current.CancellationToken);
        }
        catch (Exception ex) {
            error = ex;
        }

        watch.Stop();
        Assert.NotNull(error);
        Assert.True(watch.Elapsed < TimeSpan.FromMilliseconds(900), $"a locator timeout was retried; elapsed {watch.Elapsed}");
    }
}