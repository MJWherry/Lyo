using System.Runtime.InteropServices;
using PuppeteerSharp;

namespace Lyo.Web.WebRenderer;

public static class Utilities
{
    public const string EnvPuppeteerExecutablePath = "PUPPETEER_EXECUTABLE_PATH";
    public const string EnvChromePath = "CHROME_PATH";
    public const string EnvChromiumPath = "CHROMIUM_PATH";
    public const string EnvFirefoxPath = "FIREFOX_PATH";
    public const string EnvGoogleChromeShim = "GOOGLE_CHROME_SHIM";
    public const string EnvBrowserPath = "BROWSER_PATH";

    public static readonly Dictionary<(OSPlatform os, Architecture? arch, SupportedBrowser browser), string[]> BrowserPaths = new() {
        // Windows paths (per architecture)
        { (OSPlatform.Windows, Architecture.X64, SupportedBrowser.Chrome), [@"C:\Program Files\Google\Chrome\Application\chrome.exe"] },
        { (OSPlatform.Windows, Architecture.X86, SupportedBrowser.Chrome), [@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"] },
        { (OSPlatform.Windows, Architecture.X64, SupportedBrowser.Chromium), [@"C:\Program Files\Chromium\chrome.exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe"] }, {
            (OSPlatform.Windows, Architecture.X86, SupportedBrowser.Chromium),
            [@"C:\Program Files (x86)\Chromium\chrome.exe", @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"]
        },
        { (OSPlatform.Windows, Architecture.X64, SupportedBrowser.Firefox), [@"C:\Program Files\Mozilla Firefox\firefox.exe"] },
        { (OSPlatform.Windows, Architecture.X86, SupportedBrowser.Firefox), [@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"] },

        // Mac paths (arch ignored, so null)
        { (OSPlatform.OSX, null, SupportedBrowser.Chrome), ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/usr/local/bin/google-chrome"] },
        { (OSPlatform.OSX, null, SupportedBrowser.Chromium), ["/Applications/Chromium.app/Contents/MacOS/Chromium", "/usr/local/bin/chromium"] },
        { (OSPlatform.OSX, null, SupportedBrowser.Firefox), ["/Applications/Firefox.app/Contents/MacOS/firefox", "/usr/bin/firefox"] },

        // Linux paths (arch ignored, so null). For Chromium prefer the chromium binary, then chrome
        { (OSPlatform.Linux, null, SupportedBrowser.Chrome), ["/usr/bin/google-chrome", "/usr/bin/google-chrome-stable", "/usr/bin/chromium-browser", "/usr/bin/chromium"] }, {
            (OSPlatform.Linux, null, SupportedBrowser.Chromium),
            ["/usr/bin/chromium-browser", "/usr/bin/chromium", "/snap/bin/chromium", "/usr/bin/google-chrome", "/usr/bin/google-chrome-stable"]
        },
        { (OSPlatform.Linux, null, SupportedBrowser.Firefox), ["/usr/bin/firefox", "/snap/bin/firefox"] }
    };

    public static OSPlatform GetCurrentOsPlatform()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : throw new();

    public static string? DetectBrowserPath(SupportedBrowser browser, Architecture? arch = null)
    {
        // 1) Environment overrides first
        var envVars = browser switch {
            SupportedBrowser.Firefox => [EnvFirefoxPath, EnvBrowserPath, EnvPuppeteerExecutablePath],
            SupportedBrowser.Chrome => [EnvChromePath, EnvGoogleChromeShim, EnvBrowserPath, EnvPuppeteerExecutablePath],
            SupportedBrowser.Chromium => [EnvChromiumPath, EnvChromePath, EnvBrowserPath, EnvPuppeteerExecutablePath],
            var _ => new[] { EnvPuppeteerExecutablePath, EnvBrowserPath }
        };

        foreach (var ev in envVars) {
            var path = Environment.GetEnvironmentVariable(ev);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;
        }

        // 2) Look up the path dictionary
        var osPlatform = GetCurrentOsPlatform();

        // When architecture is set and the OS is Windows, only check that arch
        if (osPlatform.Equals(OSPlatform.Windows) && arch.HasValue) {
            if (BrowserPaths.TryGetValue((osPlatform, arch, browser), out var candidates)) {
                var found = candidates.FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }

            // Chromium with no arch-specific hit: try Chrome for the same arch
            if (browser == SupportedBrowser.Chromium && BrowserPaths.TryGetValue((osPlatform, arch, SupportedBrowser.Chrome), out var chromeCandidates)) {
                var found = chromeCandidates.FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }

            return null;
        }

        // No arch given: each OS behaves differently
        if (osPlatform.Equals(OSPlatform.Windows)) {
            // Try 64-bit, then 32-bit
            var tryOrder = new Architecture?[] { Architecture.X64, Architecture.X86 };
            foreach (var a in tryOrder) {
                if (BrowserPaths.TryGetValue((osPlatform, a, browser), out var candidates)) {
                    var found = candidates.FirstOrDefault(File.Exists);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }

                // Chromium falls back to Chrome
                if (browser == SupportedBrowser.Chromium && BrowserPaths.TryGetValue((osPlatform, a, SupportedBrowser.Chrome), out var chromeCandidates)) {
                    var found = chromeCandidates.FirstOrDefault(File.Exists);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }

            return null;
        }

        // macOS / Linux: ignore arch and look up (osPlatform, null, browser)
        if (BrowserPaths.TryGetValue((osPlatform, null, browser), out var platformCandidates)) {
            var found = platformCandidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(found))
                return found;
        }

        // Chromium falls back to Chrome (mac/linux entries)
        if (browser == SupportedBrowser.Chromium && BrowserPaths.TryGetValue((osPlatform, null, SupportedBrowser.Chrome), out var fallbackCandidates)) {
            var found = fallbackCandidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(found))
                return found;
        }

        return null;
    }
}