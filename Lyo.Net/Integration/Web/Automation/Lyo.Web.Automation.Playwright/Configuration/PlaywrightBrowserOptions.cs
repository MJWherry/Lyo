namespace Lyo.Web.Automation.Playwright.Configuration;

/// <summary>Application-wide defaults for Playwright-driven automation.</summary>
public class PlaywrightBrowserOptions
{
    public const string SectionName = "PlaywrightBrowserOptions";

    /// <summary>Which Playwright browser engine to launch.</summary>
    public PlaywrightBrowserKind BrowserKind { get; set; } = PlaywrightBrowserKind.Chromium;

    /// <summary>When true, launches headless (where supported).</summary>
    public bool Headless { get; set; }

    /// <summary>Optional channel (e.g. <c>chrome</c>, <c>msedge</c>) passed to <see cref="Microsoft.Playwright.BrowserTypeLaunchOptions.Channel" />.</summary>
    public string? Channel { get; set; }

    /// <summary>When true, strips query strings and fragments from URLs in log messages.</summary>
    public bool MaskSensitiveUrlsInLogs { get; set; }

    /// <summary>
    /// When true (default), session-local paths are rooted under <see cref="Lyo.IO.Temp.IIOTempService.CreateSession" />
    /// unless overridden. When <see cref="Lyo.IO.Temp.IIOTempService" /> is not registered, a temp folder under <see cref="Path.GetTempPath" /> is used.
    /// </summary>
    public bool UseIoTempForBrowserPaths { get; set; } = true;

    /// <summary>Explicit user data directory for persistent context; when null and <see cref="UseIoTempForBrowserPaths" /> is true, resolved under the session temp root.</summary>
    public string? BrowserUserDataDirectory { get; set; }

    /// <summary>Download directory for <see cref="Microsoft.Playwright.BrowserNewContextOptions.AcceptDownloads" />.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Artifacts directory (HAR, traces, etc.).</summary>
    public string? ArtifactsDirectory { get; set; }

    /// <summary>Extra launch arguments (Chromium/Edge).</summary>
    public List<string> LaunchArguments { get; set; } = ["disable-infobars", "disable-extensions", "disable-gpu", "disable-dev-shm-usage", "no-sandbox"];

    public List<string> UserAgents { get; set; } = [
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    ];

    /// <summary>Viewport width in CSS pixels.</summary>
    public int ViewportWidth { get; set; } = 1280;

    /// <summary>Viewport height in CSS pixels.</summary>
    public int ViewportHeight { get; set; } = 1024;

    /// <summary>Default timeout for navigations (milliseconds).</summary>
    public int NavigationTimeoutMs { get; set; } = 30_000;

    /// <summary>Default timeout for locator actions and <see cref="Lyo.Web.Automation.IWebAutomationBrowser.PollForElementAsync" /> / <see cref="Lyo.Web.Automation.IWebAutomationBrowser.PollForElementsAsync" /> (milliseconds).</summary>
    public float LocatorDefaultTimeoutMs { get; set; } = 30_000f;

    /// <summary>Maximum outer attempts for poll retries (each attempt runs one locator wait).</summary>
    public int PollingMaxAttempts { get; set; } = 5;

    /// <summary>Delay between outer poll attempts.</summary>
    public TimeSpan PollingDelayBetweenAttempts { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Whether to record metrics for browser operations.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Slow motion delay between Playwright operations (milliseconds).</summary>
    public int SlowMoMilliseconds { get; set; }

    /// <summary>Ignore HTTPS errors in the browser context.</summary>
    public bool IgnoreHttpsErrors { get; set; }

    /// <summary>When launching from <see cref="Browser.PlaywrightBrowser.StartBrowserAsync" />, close context and browser on dispose.</summary>
    public bool CloseOwnedResourcesOnDispose { get; set; } = true;

    /// <summary>Creates a deep copy for an independent session.</summary>
    public virtual PlaywrightBrowserOptions Clone()
    {
        return new PlaywrightBrowserOptions {
            BrowserKind = BrowserKind,
            Headless = Headless,
            Channel = Channel,
            MaskSensitiveUrlsInLogs = MaskSensitiveUrlsInLogs,
            UseIoTempForBrowserPaths = UseIoTempForBrowserPaths,
            BrowserUserDataDirectory = BrowserUserDataDirectory,
            DownloadDirectory = DownloadDirectory,
            ArtifactsDirectory = ArtifactsDirectory,
            LaunchArguments = [..LaunchArguments],
            UserAgents = [..UserAgents],
            ViewportWidth = ViewportWidth,
            ViewportHeight = ViewportHeight,
            NavigationTimeoutMs = NavigationTimeoutMs,
            LocatorDefaultTimeoutMs = LocatorDefaultTimeoutMs,
            PollingMaxAttempts = PollingMaxAttempts,
            PollingDelayBetweenAttempts = PollingDelayBetweenAttempts,
            EnableMetrics = EnableMetrics,
            SlowMoMilliseconds = SlowMoMilliseconds,
            IgnoreHttpsErrors = IgnoreHttpsErrors,
            CloseOwnedResourcesOnDispose = CloseOwnedResourcesOnDispose
        };
    }
}

/// <summary>Per-session overrides for <see cref="Service.IPlaywrightBrowserService.CreateSession" />.</summary>
public sealed class PlaywrightSessionOptions : PlaywrightBrowserOptions { }
