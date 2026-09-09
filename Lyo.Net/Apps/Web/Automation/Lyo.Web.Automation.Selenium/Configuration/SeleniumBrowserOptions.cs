using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Selenium.Service;
using Lyo.Web.Automation.Selenium.WebDriver;

namespace Lyo.Web.Automation.Selenium.Configuration;

/// <summary>Application-wide defaults for browser automation (timeouts, window size, polling, metrics).</summary>
/// <remarks>
/// HTTP request headers are not represented here: ChromeDriver does not apply arbitrary default headers to navigations without Chrome DevTools Protocol. Set user agent via
/// <see cref="UserAgents" /> and WebDriver/Chrome flags via <see cref="WebDriverArguments" />.
/// </remarks>
public class SeleniumBrowserOptions
{
    public const string SectionName = "SeleniumBrowserOptions";

    public List<string> UserAgents { get; set; } = [
        "Mozilla/5.0 (X11; Windows; Windows x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246",
        "Mozilla/5.0 (X11; CrOS x86_64 8172.45.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.64 Safari/537.36",
        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.111 Safari/537.36"
    ];

    /// <summary>Which browser engine to drive locally or on a remote grid.</summary>
    public SeleniumBrowserKind BrowserKind { get; set; } = SeleniumBrowserKind.Chrome;

    /// <summary>If true, starts the browser in headless mode (where supported).</summary>
    public bool Headless { get; set; }

    /// <summary>If set, uses <see cref="OpenQA.Selenium.Remote.RemoteWebDriver" /> against Selenium Grid or a standalone server.</summary>
    public string? RemoteWebDriverUri { get; set; }

    /// <summary>If true, strips query strings and fragments from URLs in log messages (tokens often live in query strings).</summary>
    public bool MaskSensitiveUrlsInLogs { get; set; }

    /// <summary>
    /// Root directory for all session subdirectories. Each session creates <c>{ServiceRootDirectory}/session-{sessionId}/</c> with <c>browser-profile/</c>, <c>artifacts/</c>,
    /// and <c>downloads/</c> inside. Default is <c>{tmp}/lyo-web-automation</c>.
    /// </summary>
    public string ServiceRootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-selenium");

    /// <summary>Explicit Chromium/Edge user-data-dir or Firefox profile directory; when null, resolved as <c>browser-profile/</c> under the session directory.</summary>
    public string? BrowserUserDataDirectory { get; set; }

    /// <summary>Download directory for browser downloads; when null, resolved as <c>downloads/</c> under the session directory.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Driver service logs and other artifacts; when null, resolved as <c>artifacts/</c> under the session directory.</summary>
    public string? ArtifactsDirectory { get; set; }

    /// <summary>
    /// Chrome WebDriver arguments. Do not add <c>disable-gpu</c> unless you need software GL: that flag forces SwiftShader, which bot-detection sites flag. Docker hosts
    /// without a GPU still use SwiftShader even without the flag; do not spoof <c>WEBGL_UNMASKED_RENDERER</c>.
    /// </summary>
    public List<string> WebDriverArguments { get; set; } = ["disable-infobars", "disable-extensions", "disable-dev-shm-usage", "no-sandbox"];

    /// <summary>
    /// JavaScript snippets injected into every new document before the page's own scripts run, via CDP <c>Page.addScriptToEvaluateOnNewDocument</c>. Only applies to Chrome and
    /// Edge. Use this to neutralize bot-detection scripts (for example anti-DevTools getter traps).
    /// </summary>
    public List<string> StartupScripts { get; set; } = [];

    /// <summary>
    /// When true (default), enables Chrome/Edge performance logging via Chrome DevTools Protocol. This is required for network request monitoring in
    /// <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationNavigator.NavigateAsync(string, Func{string, bool}, CancellationToken)" /> unless a JS request-interception
    /// startup script
    /// populates <c>window.__lyoCapturedUrls</c>. Disable on sites that detect CDP as "DevTools open" and redirect or block scraping. When disabled, add a JS interception script
    /// to
    /// <see cref="StartupScripts" /> so network requests can still be observed without CDP.
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>Browser window width passed as Chrome <c>--window-size</c> and <c>Manage().Window.Size</c>.</summary>
    public int BrowserWindowWidth { get; set; } = 1920;

    /// <summary>Browser window height passed as Chrome <c>--window-size</c> and <c>Manage().Window.Size</c>.</summary>
    public int BrowserWindowHeight { get; set; } = 1080;

    /// <summary>
    /// When true, <see cref="ApplySessionViewport" /> picks from <see cref="ViewportSizes" /> (or <see cref="DesktopDisplaySize.Common" />) once per browser start.
    /// </summary>
    public bool RandomizeViewportOnSessionStart { get; set; }

    /// <summary>Pool used when <see cref="RandomizeViewportOnSessionStart" /> is true. Empty means <see cref="DesktopDisplaySize.Common" />.</summary>
    public List<DesktopDisplaySize> ViewportSizes { get; set; } = [];

    /// <summary>Optional ±pixel jitter applied after picking a pool size. Prefer 0.</summary>
    public int ViewportJitterPixels { get; set; }

    /// <summary> Page load timeout in seconds. </summary>
    public int PageLoadTimeoutSeconds { get; set; } = 30;

    /// <summary> Implicit wait timeout in seconds. </summary>
    public int ImplicitWaitSeconds { get; set; } = 10;

    /// <summary> Script timeout in seconds. </summary>
    public int ScriptTimeoutSeconds { get; set; } = 30;

    /// <summary> Maximum wait time for Selenium operations in seconds. </summary>
    public int SeleniumMaxWaitSeconds { get; set; } = 15;

    /// <summary> Whether to record metrics for scraping operations. </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Maximum outer attempts for <see cref="SeleniumBrowser.PollFor" /> (each attempt runs one <see cref="SeleniumBrowser.WaitFor" />).</summary>
    public int PollingMaxAttempts { get; set; } = 5;

    /// <summary>Delay between outer attempts for <see cref="SeleniumBrowser.PollFor" />.</summary>
    public TimeSpan PollingDelayBetweenAttempts { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Appends a formatted entry to <see cref="WebDriverArguments" />: <c>key=value</c> when <paramref name="value" /> is non-empty; otherwise the key alone (for example
    /// <c>-headless</c>).
    /// </summary>
    public SeleniumBrowserOptions AddArgument(string key, string? value = null)
    {
        WebDriverArguments.Add(WebDriverArgumentFormatter.Format(key, value));
        return this;
    }

    /// <summary>Applies <see cref="RandomizeViewportOnSessionStart" />. Call once before creating the WebDriver so <c>--window-size</c> matches.</summary>
    public void ApplySessionViewport(Random? random = null)
    {
        if (!RandomizeViewportOnSessionStart)
            return;

        var pick = DesktopDisplaySize.Pick(ViewportSizes, ViewportJitterPixels, random);
        BrowserWindowWidth = pick.Width;
        BrowserWindowHeight = pick.Height;
    }

    /// <summary>Creates a deep copy for an independent browser session or scoped <c>SeleniumBrowser</c> instance.</summary>
    public virtual SeleniumBrowserOptions Clone()
        => new() {
            UserAgents = [.. UserAgents],
            WebDriverArguments = [.. WebDriverArguments],
            StartupScripts = [.. StartupScripts],
            EnablePerformanceLogging = EnablePerformanceLogging,
            BrowserKind = BrowserKind,
            Headless = Headless,
            RemoteWebDriverUri = RemoteWebDriverUri,
            MaskSensitiveUrlsInLogs = MaskSensitiveUrlsInLogs,
            ServiceRootDirectory = ServiceRootDirectory,
            BrowserUserDataDirectory = BrowserUserDataDirectory,
            DownloadDirectory = DownloadDirectory,
            ArtifactsDirectory = ArtifactsDirectory,
            BrowserWindowWidth = BrowserWindowWidth,
            BrowserWindowHeight = BrowserWindowHeight,
            RandomizeViewportOnSessionStart = RandomizeViewportOnSessionStart,
            ViewportSizes = [.. ViewportSizes],
            ViewportJitterPixels = ViewportJitterPixels,
            PageLoadTimeoutSeconds = PageLoadTimeoutSeconds,
            ImplicitWaitSeconds = ImplicitWaitSeconds,
            ScriptTimeoutSeconds = ScriptTimeoutSeconds,
            SeleniumMaxWaitSeconds = SeleniumMaxWaitSeconds,
            EnableMetrics = EnableMetrics,
            PollingMaxAttempts = PollingMaxAttempts,
            PollingDelayBetweenAttempts = PollingDelayBetweenAttempts
        };
}

/// <summary>Per-session browser configuration. Pass to <see cref="ISeleniumBrowserService.CreateSession" /> to override service defaults for one session.</summary>
public sealed class SeleniumSessionOptions : SeleniumBrowserOptions { }
