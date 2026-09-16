using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Selenium.Configuration;
using Lyo.Web.Automation.Selenium.Service;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace Lyo.Web.Automation.Selenium.WebDriver;

/// <summary>Constructs local or remote <see cref="IWebDriver" /> instances from <see cref="SeleniumBrowserOptions" />.</summary>
public static class WebDriverFactory
{
    /// <summary>Creates a WebDriver (local or Selenium Grid / remote).</summary>
    public static IWebDriver CreateDriver(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        ArgumentHelpers.ThrowIfNull(options);
        if (!string.IsNullOrWhiteSpace(options.RemoteWebDriverUri)) {
            var uri = new Uri(options.RemoteWebDriverUri!, UriKind.Absolute);
            return CreateRemoteDriver(uri, options, context);
        }

        return options.BrowserKind switch {
            AutomationBrowserKind.Chrome or AutomationBrowserKind.Chromium => CreateChrome(options, context),
            AutomationBrowserKind.Edge => CreateEdge(options, context),
            AutomationBrowserKind.Firefox => CreateFirefox(options, context),
            var _ => throw Unsupported(options)
        };
    }

    private static IWebDriver CreateRemoteDriver(Uri gridUri, SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        DriverOptions o = options.BrowserKind switch {
            AutomationBrowserKind.Edge => BuildEdgeOptions(options, context),
            AutomationBrowserKind.Firefox => BuildFirefoxOptions(options, context),
            AutomationBrowserKind.Chrome or AutomationBrowserKind.Chromium => BuildChromeOptions(options, context),
            var _ => throw Unsupported(options)
        };

        return new RemoteWebDriver(gridUri, o);
    }

    private static ChromeDriver CreateChrome(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildChromeOptions(options, context);
        var service = ChromeDriverService.CreateDefaultService();
        var artifactsDirectory = context?.ArtifactsDirectory;
        if (!artifactsDirectory.IsNullOrWhitespace()) {
            Directory.CreateDirectory(artifactsDirectory!);
            service.LogPath = Path.Combine(artifactsDirectory!, "chromedriver.log");
        }

        var driver = new ChromeDriver(service, o);
        InjectStartupScripts(driver, options.StartupScripts);
        return driver;
    }

    private static EdgeDriver CreateEdge(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildEdgeOptions(options, context);
        var service = EdgeDriverService.CreateDefaultService();
        var artifactsDirectory = context?.ArtifactsDirectory;
        if (!artifactsDirectory.IsNullOrWhitespace()) {
            Directory.CreateDirectory(artifactsDirectory!);
            service.LogPath = Path.Combine(artifactsDirectory!, "msedgedriver.log");
        }

        var driver = new EdgeDriver(service, o);
        InjectStartupScripts(driver, options.StartupScripts);
        return driver;
    }

    private static void InjectStartupScripts(ChromiumDriver driver, IEnumerable<string>? scripts)
    {
        if (scripts == null)
            return;

        foreach (var script in scripts.Where(s => !string.IsNullOrWhiteSpace(s)))
            driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new() { { "source", script } });
    }

    private static FirefoxDriver CreateFirefox(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = BuildFirefoxOptions(options, context);
        var service = FirefoxDriverService.CreateDefaultService();
        if (context?.ArtifactsDirectory.IsNullOrWhitespace() ?? true)
            return new(service, o);

        var ad = context.ArtifactsDirectory!;
        Directory.CreateDirectory(ad);
        service.LogPath = Path.Combine(ad, "geckodriver.log");
        return new(service, o);
    }

    private static ChromeOptions BuildChromeOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new ChromeOptions();
        foreach (var arg in options.WebDriverArguments)
            o.AddArgument(arg);

        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("--headless", "new"));

        if (!context?.BrowserUserDataDirectory.IsNullOrWhitespace() ?? false) {
            var ud = context!.BrowserUserDataDirectory!;
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-data-dir", ud));
            Directory.CreateDirectory(ud);
        }

        o.AddArgument(WebDriverArgumentFormatter.Format("--user-agent", ResolveUserAgent(options)));

        o.AddArgument(WebDriverArgumentFormatter.Format("--window-size", $"{options.BrowserWindowWidth},{options.BrowserWindowHeight}"));
        ApplyDetectedBinary(options, path => o.BinaryLocation = path);
        ApplyDownloadDirectoryChromium(o, context);
        o.SetLoggingPreference(LogType.Browser, LogLevel.All);
        if (options.EnablePerformanceLogging)
            o.SetLoggingPreference(LogType.Performance, LogLevel.All);

        return o;
    }

    private static EdgeOptions BuildEdgeOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new EdgeOptions();
        foreach (var arg in options.WebDriverArguments)
            o.AddArgument(arg);

        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("--headless", "new"));

        if (!context?.BrowserUserDataDirectory.IsNullOrWhitespace() ?? false) {
            var ud = context!.BrowserUserDataDirectory!;
            o.AddArgument(WebDriverArgumentFormatter.Format("--user-data-dir", ud));
            Directory.CreateDirectory(ud);
        }

        o.AddArgument(WebDriverArgumentFormatter.Format("--user-agent", ResolveUserAgent(options)));

        o.AddArgument(WebDriverArgumentFormatter.Format("--window-size", $"{options.BrowserWindowWidth},{options.BrowserWindowHeight}"));
        ApplyDetectedBinary(options, path => o.BinaryLocation = path);
        ApplyDownloadDirectoryChromium(o, context);
        o.SetLoggingPreference(LogType.Performance, LogLevel.All);
        return o;
    }

    private static FirefoxOptions BuildFirefoxOptions(SeleniumBrowserOptions options, SeleniumExecutionContext? context)
    {
        var o = new FirefoxOptions();
        ApplyDetectedBinary(options, path => o.BinaryLocation = path);
        if (options.Headless)
            o.AddArgument(WebDriverArgumentFormatter.Format("-headless", null));

        o.AddArgument(WebDriverArgumentFormatter.Format("--width", options.BrowserWindowWidth.ToString()));
        o.AddArgument(WebDriverArgumentFormatter.Format("--height", options.BrowserWindowHeight.ToString()));
        if (!context?.BrowserUserDataDirectory.IsNullOrWhitespace() ?? false) {
            var ud = context!.BrowserUserDataDirectory!;
            Directory.CreateDirectory(ud);
            o.Profile = new(ud);
        }

        if (!context?.DownloadDirectory.IsNullOrWhitespace() ?? false) {
            var dd = context!.DownloadDirectory!;
            Directory.CreateDirectory(dd);
            o.SetPreference("browser.download.folderList", 2);
            o.SetPreference("browser.download.dir", dd);
            o.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/octet-stream");
        }

        o.SetPreference("general.useragent.override", ResolveUserAgent(options));

        return o;
    }

    private static string ResolveUserAgent(SeleniumBrowserOptions options)
        => options.UserAgents.Count == 0
            ? AutomationBrowserDetector.UserAgent(options.BrowserKind)
            : options.UserAgents[new Random().Next(options.UserAgents.Count)];

    private static void ApplyDetectedBinary(SeleniumBrowserOptions options, Action<string> setBinary)
    {
        if (!string.IsNullOrWhiteSpace(options.RemoteWebDriverUri))
            return;

        var path = AutomationBrowserDetector.Detect(options.BrowserKind);
        if (path != null)
            setBinary(path);
    }

    private static ConfigurationException Unsupported(SeleniumBrowserOptions options)
        => new($"Selenium cannot launch {options.BrowserKind}.", nameof(SeleniumBrowserOptions.BrowserKind));

    private static void ApplyDownloadDirectoryChromium(ChromiumOptions o, SeleniumExecutionContext? context)
    {
        if (context?.DownloadDirectory.IsNullOrWhitespace() ?? true)
            return;

        var dir = context.DownloadDirectory!;
        Directory.CreateDirectory(dir);
        o.AddUserProfilePreference("download.default_directory", dir);
        o.AddUserProfilePreference("download.prompt_for_download", false);
        o.AddUserProfilePreference("download.directory_upgrade", true);
    }
}