using Lyo.Exceptions;
using Lyo.Web.Automation.Selenium.Configuration;
using Lyo.Web.Automation.Selenium.Service;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace Lyo.Web.Automation.Selenium.WebDriver;

/// <summary>Builds local or remote <see cref="IWebDriver" /> instances from <see cref="SeleniumBrowserOptions" />.</summary>
public static class WebDriverFactory
{
    /// <summary>Creates a WebDriver (local or Selenium Grid / remote).</summary>
    public static IWebDriver CreateDriver(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.RemoteWebDriverUri)) {
            var uri = new Uri(options.RemoteWebDriverUri!, UriKind.Absolute);
            return CreateRemoteDriver(uri, options, context);
        }

        return options.BrowserKind switch {
            SeleniumBrowserKind.Chrome => CreateChrome(options, context),
            SeleniumBrowserKind.Edge => CreateEdge(options, context),
            SeleniumBrowserKind.Firefox => CreateFirefox(options, context),
            _ => CreateChrome(options, context)
        };
    }

    private static IWebDriver CreateRemoteDriver(Uri gridUri, SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        DriverOptions o = options.BrowserKind switch {
            SeleniumBrowserKind.Edge => BuildEdgeOptions(options, context),
            SeleniumBrowserKind.Firefox => BuildFirefoxOptions(options, context),
            _ => BuildChromeOptions(options, context)
        };

        return new RemoteWebDriver(gridUri, o);
    }

    private static ChromeDriver CreateChrome(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildChromeOptions(options, context);
        var service = ChromeDriverService.CreateDefaultService();
        if (!string.IsNullOrWhiteSpace(context?.ArtifactsDirectory)) {
            var ad = context!.ArtifactsDirectory!;
            Directory.CreateDirectory(ad);
            service.LogPath = Path.Combine(ad, "chromedriver.log");
        }

        var driver = new ChromeDriver(service, o);
        InjectStartupScripts(driver, options.StartupScripts);
        return driver;
    }

    private static EdgeDriver CreateEdge(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildEdgeOptions(options, context);
        var service = EdgeDriverService.CreateDefaultService();
        if (!string.IsNullOrWhiteSpace(context?.ArtifactsDirectory)) {
            var ad = context!.ArtifactsDirectory!;
            Directory.CreateDirectory(ad);
            service.LogPath = Path.Combine(ad, "msedgedriver.log");
        }

        var driver = new EdgeDriver(service, o);
        InjectStartupScripts(driver, options.StartupScripts);
        return driver;
    }

    private static void InjectStartupScripts(OpenQA.Selenium.Chromium.ChromiumDriver driver, IEnumerable<string>? scripts)
    {
        if (scripts == null)
            return;

        foreach (var script in scripts.Where(s => !string.IsNullOrWhiteSpace(s))) {
            driver.ExecuteCdpCommand(
                "Page.addScriptToEvaluateOnNewDocument",
                new Dictionary<string, object?> { { "source", script } });
        }
    }

    private static FirefoxDriver CreateFirefox(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildFirefoxOptions(options, context);
        var service = FirefoxDriverService.CreateDefaultService();
        if (!string.IsNullOrWhiteSpace(context?.ArtifactsDirectory)) {
            var ad = context!.ArtifactsDirectory!;
            Directory.CreateDirectory(ad);
            service.LogPath = Path.Combine(ad, "geckodriver.log");
        }

        return new FirefoxDriver(service, o);
    }

    private static ChromeOptions BuildChromeOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new ChromeOptions();
        foreach (var arg in options.WebDriverArguments)
            o.AddArgument(arg);

        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("--headless", "new"));

        if (!string.IsNullOrWhiteSpace(context?.BrowserUserDataDirectory)) {
            var ud = context!.BrowserUserDataDirectory!;
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-data-dir", ud));
            Directory.CreateDirectory(ud);
        }

        if (options.UserAgents.Count > 0) {
            var ua = options.UserAgents[new Random().Next(options.UserAgents.Count)];
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-agent", ua));
        }

        o.AddArgument(
            WebDriverArgumentFormatter.Format(
                "--window-size",
                $"{options.BrowserWindowWidth},{options.BrowserWindowHeight}"));
        ApplyDownloadDirectoryChromium(o, context);
        o.SetLoggingPreference(LogType.Browser, OpenQA.Selenium.LogLevel.All);
        if (options.EnablePerformanceLogging)
            o.SetLoggingPreference(LogType.Performance, OpenQA.Selenium.LogLevel.All);
        return o;
    }

    private static EdgeOptions BuildEdgeOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new EdgeOptions();
        foreach (var arg in options.WebDriverArguments)
            o.AddArgument(arg);

        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("--headless", "new"));

        if (!string.IsNullOrWhiteSpace(context?.BrowserUserDataDirectory)) {
            var ud = context!.BrowserUserDataDirectory!;
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-data-dir", ud));
            Directory.CreateDirectory(ud);
        }

        if (options.UserAgents.Count > 0) {
            var ua = options.UserAgents[new Random().Next(options.UserAgents.Count)];
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-agent", ua));
        }

        o.AddArgument(
            WebDriverArgumentFormatter.Format(
                "--window-size",
                $"{options.BrowserWindowWidth},{options.BrowserWindowHeight}"));
        ApplyDownloadDirectoryChromium(o, context);
        o.SetLoggingPreference(LogType.Performance, OpenQA.Selenium.LogLevel.All);
        return o;
    }

    private static FirefoxOptions BuildFirefoxOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new FirefoxOptions();
        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("-headless", null));

        o.AddArgument(WebDriverArgumentFormatter.Format("--width", options.BrowserWindowWidth.ToString()));
        o.AddArgument(WebDriverArgumentFormatter.Format("--height", options.BrowserWindowHeight.ToString()));

        if (!string.IsNullOrWhiteSpace(context?.BrowserUserDataDirectory)) {
            var ud = context!.BrowserUserDataDirectory!;
            Directory.CreateDirectory(ud);
            o.Profile = new FirefoxProfile(ud);
        }

        if (!string.IsNullOrWhiteSpace(context?.DownloadDirectory)) {
            var dd = context!.DownloadDirectory!;
            Directory.CreateDirectory(dd);
            o.SetPreference("browser.download.folderList", 2);
            o.SetPreference("browser.download.dir", dd);
            o.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/octet-stream");
        }

        if (options.UserAgents.Count > 0) {
            var ua = options.UserAgents[new Random().Next(options.UserAgents.Count)];
            o.SetPreference("general.useragent.override", ua);
        }

        return o;
    }

    private static void ApplyDownloadDirectoryChromium(ChromiumOptions o, SeleniumExecutionContext? context)
    {
        if (string.IsNullOrWhiteSpace(context?.DownloadDirectory))
            return;

        var dir = context!.DownloadDirectory!;
        Directory.CreateDirectory(dir);
        o.AddUserProfilePreference("download.default_directory", dir);
        o.AddUserProfilePreference("download.prompt_for_download", false);
        o.AddUserProfilePreference("download.directory_upgrade", true);
    }
}
