using System.Runtime.InteropServices;
using Lyo.Common.Core.Net;

namespace Lyo.Web.Automation.Models;

/// <summary>
/// Finds an installed browser from <see cref="Catalog" />. Checks that row's environment variables, then <c>BROWSER_PATH</c> and <c>PUPPETEER_EXECUTABLE_PATH</c>, then
/// install paths for the current OS. Windows tries x64, then x86. The first existing file wins.
/// </summary>
public static class AutomationBrowserDetector
{
    private const string EnvBrowserPath = "BROWSER_PATH";
    private const string EnvPuppeteerExecutablePath = "PUPPETEER_EXECUTABLE_PATH";

    /// <summary>Browser name, environment variables, and install paths for every <see cref="AutomationBrowserKind" />.</summary>
    public static IReadOnlyList<AutomationBrowserMetadata> Catalog { get; } = CreateCatalog();

    /// <summary>User-Agent for <paramref name="kind" /> on this host, from that row's template.</summary>
    public static string UserAgent(AutomationBrowserKind kind)
    {
        var template = Find(kind).UserAgent;
        return UserAgentGenerator.Generate(template.Format, template.Version);
    }

    /// <summary>Returns the executable for <paramref name="kind" />, or null when this host has no matching file. WebKit always returns null.</summary>
    public static string? Detect(AutomationBrowserKind kind)
    {
        var metadata = Find(kind);
        if (metadata.EnvironmentVariables.Count == 0 && metadata.Paths.Count == 0)
            return null;

        foreach (var name in metadata.EnvironmentVariables) {
            var fromKind = ExistingFile(Environment.GetEnvironmentVariable(name));
            if (fromKind != null)
                return fromKind;
        }

        var shared = ExistingFile(Environment.GetEnvironmentVariable(EnvBrowserPath)) ?? ExistingFile(Environment.GetEnvironmentVariable(EnvPuppeteerExecutablePath));
        if (shared != null)
            return shared;

        return FirstInstalled(metadata);
    }

    private static AutomationBrowserMetadata Find(AutomationBrowserKind kind)
    {
        foreach (var row in Catalog) {
            if (row.Kind == kind)
                return row;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    private static string? FirstInstalled(AutomationBrowserMetadata metadata)
    {
        var os = CurrentOs();
        if (os is null)
            return null;

        if (os.Value.Equals(OSPlatform.Windows)) {
            foreach (var arch in new[] { Architecture.X64, Architecture.X86 }) {
                var found = FirstMatch(metadata, os.Value, arch);
                if (found != null)
                    return found;
            }

            return null;
        }

        return FirstMatch(metadata, os.Value, null);
    }

    private static string? FirstMatch(AutomationBrowserMetadata metadata, OSPlatform os, Architecture? architecture)
    {
        foreach (var row in metadata.Paths) {
            if (!row.OperatingSystem.Equals(os) || row.Architecture != architecture)
                continue;

            if (File.Exists(row.Path))
                return row.Path;
        }

        return null;
    }

    private static OSPlatform? CurrentOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;

        return null;
    }

    private static string? ExistingFile(string? path) => !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;

    private static IReadOnlyList<AutomationBrowserMetadata> CreateCatalog()
    {
        var chrome = new AutomationBrowserUserAgentTemplate(UserAgentFormat.Chromium, UserAgentGenerator.ChromeVersion);
        var edge = new AutomationBrowserUserAgentTemplate(UserAgentFormat.Edge, UserAgentGenerator.ChromeVersion);
        var firefox = new AutomationBrowserUserAgentTemplate(UserAgentFormat.Firefox, UserAgentGenerator.FirefoxVersion);
        var safari = new AutomationBrowserUserAgentTemplate(UserAgentFormat.Safari, UserAgentGenerator.SafariVersion);
        return [
            new(
                AutomationBrowserKind.Chrome, "Google Chrome", ["CHROME_PATH", "GOOGLE_CHROME_SHIM"],
                [
                    new(OSPlatform.Windows, Architecture.X64, @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
                    new(OSPlatform.Windows, Architecture.X86, @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"),
                    new(OSPlatform.OSX, null, "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"), new(OSPlatform.OSX, null, "/usr/local/bin/google-chrome"),
                    new(OSPlatform.Linux, null, "/usr/bin/google-chrome"), new(OSPlatform.Linux, null, "/usr/bin/google-chrome-stable"),
                    new(OSPlatform.Linux, null, "/usr/bin/chromium-browser"), new(OSPlatform.Linux, null, "/usr/bin/chromium")
                ], chrome),
            new(
                AutomationBrowserKind.Chromium, "Chromium", ["CHROMIUM_PATH", "CHROME_PATH"],
                [
                    new(OSPlatform.Windows, Architecture.X64, @"C:\Program Files\Chromium\chrome.exe"),
                    new(OSPlatform.Windows, Architecture.X64, @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
                    new(OSPlatform.Windows, Architecture.X86, @"C:\Program Files (x86)\Chromium\chrome.exe"),
                    new(OSPlatform.Windows, Architecture.X86, @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"),
                    new(OSPlatform.OSX, null, "/Applications/Chromium.app/Contents/MacOS/Chromium"), new(OSPlatform.OSX, null, "/usr/local/bin/chromium"),
                    new(OSPlatform.Linux, null, "/usr/bin/chromium-browser"), new(OSPlatform.Linux, null, "/usr/bin/chromium"), new(OSPlatform.Linux, null, "/snap/bin/chromium"),
                    new(OSPlatform.Linux, null, "/usr/bin/google-chrome"), new(OSPlatform.Linux, null, "/usr/bin/google-chrome-stable")
                ], chrome),
            new(
                AutomationBrowserKind.Edge, "Microsoft Edge", ["EDGE_PATH"],
                [
                    new(OSPlatform.Windows, Architecture.X64, @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
                    new(OSPlatform.Windows, Architecture.X86, @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
                    new(OSPlatform.OSX, null, "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"), new(OSPlatform.Linux, null, "/usr/bin/microsoft-edge"),
                    new(OSPlatform.Linux, null, "/usr/bin/microsoft-edge-stable")
                ], edge),
            new(
                AutomationBrowserKind.Firefox, "Firefox", ["FIREFOX_PATH"],
                [
                    new(OSPlatform.Windows, Architecture.X64, @"C:\Program Files\Mozilla Firefox\firefox.exe"),
                    new(OSPlatform.Windows, Architecture.X86, @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"),
                    new(OSPlatform.OSX, null, "/Applications/Firefox.app/Contents/MacOS/firefox"), new(OSPlatform.OSX, null, "/usr/bin/firefox"),
                    new(OSPlatform.Linux, null, "/usr/bin/firefox"), new(OSPlatform.Linux, null, "/snap/bin/firefox")
                ], firefox),
            new(AutomationBrowserKind.WebKit, "WebKit", [], [], safari)
        ];
    }
}