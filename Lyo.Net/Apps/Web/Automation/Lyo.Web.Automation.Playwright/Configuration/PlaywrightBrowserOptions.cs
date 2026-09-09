using Lyo.Exceptions;
using Lyo.Web.Automation.Models;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Configuration;

/// <summary>Application-wide defaults for Playwright-driven automation.</summary>
public class PlaywrightBrowserOptions
{
    public const string SectionName = "PlaywrightBrowserOptions";

    /// <summary>Which Playwright browser engine to launch.</summary>
    public PlaywrightBrowserKind BrowserKind { get; set; } = PlaywrightBrowserKind.Chromium;

    /// <summary>If true, launches headless (where supported).</summary>
    public bool Headless { get; set; }

    /// <summary>Optional Playwright channel (<c>chrome</c>, <c>msedge</c>). Applied only when <see cref="BrowserKind" /> is <see cref="PlaywrightBrowserKind.Chromium" />.</summary>
    public string? Channel { get; set; }

    /// <summary>HTTP proxy for the browser context (same exit as Flared when handing off <c>cf_clearance</c>).</summary>
    public string? ProxyUrl { get; set; }

    /// <summary>Proxy username when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyUsername { get; set; }

    /// <summary>Proxy password when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyPassword { get; set; }

    /// <summary>If true, strips query strings and fragments from URLs in log messages.</summary>
    public bool MaskSensitiveUrlsInLogs { get; set; }

    /// <summary>
    /// Root directory for all session subdirectories. Each session creates <c>{ServiceRootDirectory}/session-{sessionId}/</c> with <c>browser-profile/</c>, <c>artifacts/</c>,
    /// and <c>downloads/</c> inside. Default is <c>{tmp}/lyo-web-automation</c>.
    /// </summary>
    public string ServiceRootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-playwright");

    /// <summary>
    /// Session profile directory; when null, resolved as <c>browser-profile/</c> under the session directory. Not passed as a Playwright persistent context in this
    /// package.
    /// </summary>
    public string? BrowserUserDataDirectory { get; set; }

    /// <summary>Download directory; when null, resolved as <c>downloads/</c> under the session directory.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Artifacts directory (HAR, traces); when null, resolved as <c>artifacts/</c> under the session directory.</summary>
    public string? ArtifactsDirectory { get; set; }

    /// <summary>Chromium-only defaults. Do not pass these to Firefox or WebKit; Playwright treats unknown tokens as start URLs (for example <c>http://1920,1080</c>).</summary>
    public static readonly string[] ChromiumDefaultLaunchArguments = ["--disable-infobars", "--disable-extensions", "--disable-dev-shm-usage", "--no-sandbox"];

    /// <summary>
    /// Extra launch arguments (Chromium/Edge). Each entry must start with <c>-</c>; Playwright rejects bare tokens (it treats them as an initial page URL). Do not add
    /// <c>--disable-gpu</c> unless you need software GL: that flag forces SwiftShader (<c>SwiftShader Device (Subzero)</c>), which bot-detection sites flag. Docker hosts without a
    /// GPU still use SwiftShader even without the flag; do not spoof <c>WEBGL_UNMASKED_RENDERER</c>. <see cref="ResolveLaunchArguments" /> omits these defaults (and
    /// <c>--window-size</c>) when <see cref="BrowserKind" /> is not Chromium.
    /// </summary>
    public List<string> LaunchArguments { get; set; } = [.. ChromiumDefaultLaunchArguments];

    /// <summary>Default Chromium User-Agent. Firefox/WebKit skip this pool and use the engine UA unless the host replaces <see cref="UserAgents" />.</summary>
    public const string DefaultChromiumUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>User-Agent pool. Playwright uses the first entry. Firefox/WebKit omit a context UA when this is still the Chromium default.</summary>
    public List<string> UserAgents { get; set; } = [DefaultChromiumUserAgent];

    /// <summary>Viewport width in CSS pixels (<c>window.innerWidth</c>).</summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>Viewport height in CSS pixels (<c>window.innerHeight</c>).</summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>
    /// Reported as <c>window.screen.width</c>. Headless Chromium defaults to 800 when this is omitted; Playwright requires it to be ≥ <see cref="ViewportWidth" />.
    /// </summary>
    public int ScreenWidth { get; set; } = 1920;

    /// <summary>
    /// Reported as <c>window.screen.height</c>. Headless Chromium defaults to 600 when this is omitted; Playwright requires it to be ≥ <see cref="ViewportHeight" />.
    /// </summary>
    public int ScreenHeight { get; set; } = 1080;

    /// <summary>
    /// When true, <see cref="ApplySessionViewport" /> picks from <see cref="ViewportSizes" /> (or <see cref="DesktopDisplaySize.Common" />) once per browser start and assigns
    /// both viewport and screen so the session looks maximized.
    /// </summary>
    public bool RandomizeViewportOnSessionStart { get; set; }

    /// <summary>Pool used when <see cref="RandomizeViewportOnSessionStart" /> is true. Empty means <see cref="DesktopDisplaySize.Common" />.</summary>
    public List<DesktopDisplaySize> ViewportSizes { get; set; } = [];

    /// <summary>Optional ±pixel jitter applied after picking a pool size. Prefer 0; discrete common sizes look less synthetic than a 1px jitter.</summary>
    public int ViewportJitterPixels { get; set; }

    /// <summary>
    /// Playwright <c>waitUntil</c> for goto/reload. Default <see cref="WaitUntilState.DOMContentLoaded" /> so Cloudflare interstitials that never fire <c>window.load</c> do not
    /// time out. Set <see cref="WaitUntilState.Load" /> when the page must finish every subresource.
    /// </summary>
    public WaitUntilState NavigationWaitUntil { get; set; } = WaitUntilState.DOMContentLoaded;

    /// <summary>Default timeout for navigations (milliseconds).</summary>
    public int NavigationTimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Default timeout for locator actions and <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationPage.PollForElementAsync" /> /
    /// <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationPage.PollForElementsAsync" /> (milliseconds).
    /// </summary>
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

    /// <summary>
    /// Applies <see cref="RandomizeViewportOnSessionStart" /> and ensures <see cref="ScreenWidth" />×<see cref="ScreenHeight" /> is at least the viewport (Playwright rejects a
    /// smaller screen). Call once before launch.
    /// </summary>
    public void ApplySessionViewport(Random? random = null)
    {
        if (RandomizeViewportOnSessionStart) {
            var pick = DesktopDisplaySize.Pick(ViewportSizes, ViewportJitterPixels, random);
            ViewportWidth = pick.Width;
            ViewportHeight = pick.Height;
            ScreenWidth = pick.Width;
            ScreenHeight = pick.Height;
            return;
        }

        if (ScreenWidth < ViewportWidth)
            ScreenWidth = ViewportWidth;
        if (ScreenHeight < ViewportHeight)
            ScreenHeight = ViewportHeight;
    }

    /// <summary>
    /// Launch args for this engine. Chromium gets <c>--window-size</c> matching the screen when missing. Firefox/WebKit drop Chromium defaults and never get
    /// <c>--window-size</c> (Playwright opens that token as a URL). Every remaining entry must start with <c>-</c>.
    /// </summary>
    public IReadOnlyList<string> ResolveLaunchArguments()
    {
        List<string> args;
        if (BrowserKind != PlaywrightBrowserKind.Chromium) {
            args = [];
            foreach (var arg in LaunchArguments) {
                if (!IsChromiumBuiltinLaunchArg(arg))
                    args.Add(arg);
            }
        }
        else {
            args = [.. LaunchArguments];
            var hasWindowSize = false;
            foreach (var arg in args) {
                if (arg.StartsWith("--window-size=", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--window-size", StringComparison.OrdinalIgnoreCase)) {
                    hasWindowSize = true;
                    break;
                }
            }

            if (!hasWindowSize)
                args.Add($"--window-size={ScreenWidth},{ScreenHeight}");
        }

        foreach (var arg in args)
            EnsureLaunchArgIsFlag(arg);

        return args;
    }

    /// <summary>User-Agent for a new context, or <see langword="null" /> to keep the engine default (Firefox/WebKit with the Chromium default pool).</summary>
    public string? ResolveContextUserAgent()
    {
        if (UserAgents.Count == 0)
            return null;

        if (BrowserKind != PlaywrightBrowserKind.Chromium && IsDefaultChromiumUserAgentPool())
            return null;

        return UserAgents[0];
    }

    private bool IsDefaultChromiumUserAgentPool()
        => UserAgents.Count == 1 && string.Equals(UserAgents[0], DefaultChromiumUserAgent, StringComparison.Ordinal);

    private static void EnsureLaunchArgIsFlag(string arg)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(arg);
        ArgumentHelpers.ThrowIf(
            !arg.StartsWith("-", StringComparison.Ordinal),
            $"Launch argument '{arg}' must start with '-'; Playwright treats other tokens as a start URL.");
    }

    private static bool IsChromiumBuiltinLaunchArg(string arg)
    {
        if (arg.StartsWith("--window-size=", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--window-size", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var builtin in ChromiumDefaultLaunchArguments) {
            if (string.Equals(builtin, arg, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Creates a deep copy for an independent session.</summary>
    public virtual PlaywrightBrowserOptions Clone()
        => new() {
            BrowserKind = BrowserKind,
            Headless = Headless,
            Channel = Channel,
            ProxyUrl = ProxyUrl,
            ProxyUsername = ProxyUsername,
            ProxyPassword = ProxyPassword,
            MaskSensitiveUrlsInLogs = MaskSensitiveUrlsInLogs,
            ServiceRootDirectory = ServiceRootDirectory,
            BrowserUserDataDirectory = BrowserUserDataDirectory,
            DownloadDirectory = DownloadDirectory,
            ArtifactsDirectory = ArtifactsDirectory,
            LaunchArguments = [.. LaunchArguments],
            UserAgents = [.. UserAgents],
            ViewportWidth = ViewportWidth,
            ViewportHeight = ViewportHeight,
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            RandomizeViewportOnSessionStart = RandomizeViewportOnSessionStart,
            ViewportSizes = [.. ViewportSizes],
            ViewportJitterPixels = ViewportJitterPixels,
            NavigationWaitUntil = NavigationWaitUntil,
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

/// <summary>Per-session overrides for <see cref="Service.IPlaywrightBrowserService.CreateSession(PlaywrightSessionOptions?)" />.</summary>
public sealed class PlaywrightSessionOptions : PlaywrightBrowserOptions { }
