using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Web.Automation.Models;
using PuppeteerSharp;

namespace Lyo.Web.Automation.PuppeteerSharp.Configuration;

/// <summary>Application-wide defaults for PuppeteerSharp automation. Section name is <see cref="SectionName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PuppeteerSharpBrowserOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PuppeteerSharpBrowserOptions";

    /// <summary>Chromium launch defaults. Every entry starts with <c>-</c>.</summary>
    public static readonly string[] ChromiumDefaultLaunchArguments = ["--disable-infobars", "--disable-extensions", "--disable-dev-shm-usage", "--no-sandbox"];

    /// <summary>
    /// Which browser to launch. Chrome, Chromium, Edge, and Firefox resolve an executable through <see cref="AutomationBrowserDetector" /> unless <see cref="BrowserExePath" />
    /// is set. WebKit is rejected by <see cref="Validate" />.
    /// </summary>
    public AutomationBrowserKind BrowserKind { get; set; } = AutomationBrowserKind.Chrome;

    /// <summary>Executable override. When null or empty, launch uses <see cref="AutomationBrowserDetector.Detect" /> for <see cref="BrowserKind" />.</summary>
    public string? BrowserExePath { get; set; }

    /// <summary>When true, launches headless.</summary>
    public bool Headless { get; set; }

    /// <summary>HTTP proxy for the browser context.</summary>
    public string? ProxyUrl { get; set; }

    /// <summary>Proxy username when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyUsername { get; set; }

    /// <summary>Proxy password when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyPassword { get; set; }

    /// <summary>When true, strips query strings and fragments from URLs in log messages.</summary>
    public bool MaskSensitiveUrlsInLogs { get; set; }

    /// <summary>Root directory for session folders. Default is <c>{tmp}/lyo-puppeteersharp</c>.</summary>
    public string ServiceRootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-puppeteersharp");

    /// <summary>Profile directory. When null, resolved as <c>browser-profile/</c> under the session directory and passed as Chrome's user-data-dir.</summary>
    public string? BrowserUserDataDirectory { get; set; }

    /// <summary>Download directory. When null, resolved as <c>downloads/</c> under the session directory.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Artifacts directory. When null, resolved as <c>artifacts/</c> under the session directory.</summary>
    public string? ArtifactsDirectory { get; set; }

    /// <summary>Extra Chrome arguments. Each entry must start with <c>-</c>. <see cref="ResolveLaunchArguments" /> adds <c>--window-size</c> when missing.</summary>
    public List<string> LaunchArguments { get; set; } = [.. ChromiumDefaultLaunchArguments];

    /// <summary>User-Agent pool. The first entry is applied to each new page. An empty list is generated for <see cref="BrowserKind" /> on this host.</summary>
    public List<string> UserAgents { get; set; } = [];

    /// <summary>Viewport width in CSS pixels.</summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>Viewport height in CSS pixels.</summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>Reported screen width. <see cref="ApplySessionViewport" /> raises this to at least <see cref="ViewportWidth" />.</summary>
    public int ScreenWidth { get; set; } = 1920;

    /// <summary>Reported screen height. <see cref="ApplySessionViewport" /> raises this to at least <see cref="ViewportHeight" />.</summary>
    public int ScreenHeight { get; set; } = 1080;

    /// <summary>When true, <see cref="ApplySessionViewport" /> picks one size from <see cref="ViewportSizes" /> (or <see cref="DesktopDisplaySize.Common" />) per browser start.</summary>
    public bool RandomizeViewportOnSessionStart { get; set; }

    /// <summary>Pool used when <see cref="RandomizeViewportOnSessionStart" /> is true. Empty means <see cref="DesktopDisplaySize.Common" />.</summary>
    public List<DesktopDisplaySize> ViewportSizes { get; set; } = [];

    /// <summary>Optional ±pixel jitter applied after picking a pool size.</summary>
    public int ViewportJitterPixels { get; set; }

    /// <summary>When a navigation counts as finished. Starts at DOMContentLoaded so a page that never fires <c>load</c> still returns.</summary>
    public WaitUntilNavigation NavigationWaitUntil { get; set; } = WaitUntilNavigation.DOMContentLoaded;

    /// <summary>Navigation timeout in milliseconds.</summary>
    public int NavigationTimeoutMs { get; set; } = 30_000;

    /// <summary>Timeout for element waits and polls, in milliseconds.</summary>
    public float LocatorDefaultTimeoutMs { get; set; } = 30_000f;

    /// <summary>Maximum outer attempts for poll retries.</summary>
    public int PollingMaxAttempts { get; set; } = 5;

    /// <summary>Delay between outer poll attempts.</summary>
    public TimeSpan PollingDelayBetweenAttempts { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Whether to record metrics for browser operations.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Delay between Puppeteer input operations, in milliseconds.</summary>
    public int SlowMoMilliseconds { get; set; }

    /// <summary>Ignore HTTPS errors.</summary>
    public bool IgnoreHttpsErrors { get; set; }

    /// <summary>When this instance launched the browser, close it on dispose.</summary>
    public bool CloseOwnedResourcesOnDispose { get; set; } = true;

    /// <summary>Checks viewport, timeouts, and that a set <see cref="BrowserExePath" /> exists on disk.</summary>
    public void Validate()
    {
        if (BrowserKind == AutomationBrowserKind.WebKit)
            throw new ConfigurationException("PuppeteerSharp cannot launch WebKit.", nameof(BrowserKind));

        if (ViewportWidth < 1)
            throw new ConfigurationException("ViewportWidth must be at least 1.", nameof(ViewportWidth));

        if (ViewportHeight < 1)
            throw new ConfigurationException("ViewportHeight must be at least 1.", nameof(ViewportHeight));

        if (NavigationTimeoutMs < 1)
            throw new ConfigurationException("NavigationTimeoutMs must be at least 1.", nameof(NavigationTimeoutMs));

        if (LocatorDefaultTimeoutMs <= 0)
            throw new ConfigurationException("LocatorDefaultTimeoutMs must be positive.", nameof(LocatorDefaultTimeoutMs));

        if (PollingMaxAttempts < 1)
            throw new ConfigurationException("PollingMaxAttempts must be at least 1.", nameof(PollingMaxAttempts));

        if (PollingDelayBetweenAttempts < TimeSpan.Zero)
            throw new ConfigurationException("PollingDelayBetweenAttempts cannot be negative.", nameof(PollingDelayBetweenAttempts));

        if (!string.IsNullOrWhiteSpace(BrowserExePath) && !File.Exists(BrowserExePath))
            throw new ConfigurationException($"BrowserExePath '{BrowserExePath}' does not exist.", nameof(BrowserExePath));
    }

    /// <summary>Applies <see cref="RandomizeViewportOnSessionStart" /> and ensures the screen is at least the viewport. Call once before launch.</summary>
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
    /// Launch args for this browser. Chromium-family kinds get <c>--window-size</c> matching the screen when missing. Firefox drops Chromium defaults and never gets
    /// <c>--window-size</c>. Every remaining entry must start with <c>-</c>.
    /// </summary>
    public IReadOnlyList<string> ResolveLaunchArguments()
    {
        List<string> args;
        if (BrowserKind == AutomationBrowserKind.Firefox) {
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

    /// <summary>User-Agent for a new page. An empty <see cref="UserAgents" /> list is generated for <see cref="BrowserKind" /> on this host.</summary>
    public string ResolveContextUserAgent() => UserAgents.Count == 0 ? AutomationBrowserDetector.UserAgent(BrowserKind) : UserAgents[0];

    /// <summary>Creates a deep copy for an independent session.</summary>
    public virtual PuppeteerSharpBrowserOptions Clone()
        => new() {
            BrowserKind = BrowserKind,
            BrowserExePath = BrowserExePath,
            Headless = Headless,
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

    private static void EnsureLaunchArgIsFlag(string arg)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(arg);
        ArgumentHelpers.ThrowIf(!arg.StartsWith("-", StringComparison.Ordinal), $"Launch argument '{arg}' must start with '-'.");
    }

    /// <summary>Browser executable, proxy, and navigation limits. The proxy password is omitted.</summary>
    public override string ToString() =>
        $"Browser={BrowserKind} Exe={BrowserExePath} Headless={Headless} Proxy={ProxyUrl} ProxyPasswordSet={!string.IsNullOrWhiteSpace(ProxyPassword)} " +
        $"Viewport={ViewportWidth}x{ViewportHeight} NavTimeoutMs={NavigationTimeoutMs} Agents={UserAgents?.Count ?? 0}";
}

/// <summary>Per-session overrides for <see cref="Service.IPuppeteerSharpBrowserService.CreateSession(PuppeteerSharpSessionOptions?)" />.</summary>
public sealed class PuppeteerSharpSessionOptions : PuppeteerSharpBrowserOptions { }