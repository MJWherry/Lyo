using Lyo.IO.Temp;
using Lyo.Web.Automation.Selenium.Configuration;

namespace Lyo.Web.Automation.Selenium.Service;

internal static class SeleniumExecutionContextFactory
{
    public static SeleniumExecutionContext Create(SeleniumBrowserOptions options, IIOTempService? ioTemp, Guid sessionId)
    {
        if (!options.UseIoTempForBrowserPaths) {
            EnsureOptionalDirs(options.BrowserUserDataDirectory, options.ArtifactsDirectory, options.DownloadDirectory);
            return new SeleniumExecutionContext {
                SessionId = sessionId,
                BrowserUserDataDirectory = options.BrowserUserDataDirectory,
                DownloadDirectory = options.DownloadDirectory,
                ArtifactsDirectory = options.ArtifactsDirectory,
                IoTempSession = null,
                FallbackTempRoot = null
            };
        }

        var io = ioTemp?.CreateSession();
        string root;
        string? fallback = null;
        if (io != null) {
            root = io.SessionDirectory;
        }
        else {
            root = Path.Combine(Path.GetTempPath(), "lyo-scraping", sessionId.ToString("N"));
            Directory.CreateDirectory(root);
            fallback = root;
        }

        string OrSub(string? explicitPath, string defaultSegment) 
            => !string.IsNullOrWhiteSpace(explicitPath) ? explicitPath! : Path.Combine(root, defaultSegment);

        var browser = OrSub(options.BrowserUserDataDirectory, "browser-profile");
        var artifacts = OrSub(options.ArtifactsDirectory, "artifacts");
        var downloads = OrSub(options.DownloadDirectory, "downloads");
        Directory.CreateDirectory(browser);
        Directory.CreateDirectory(artifacts);
        Directory.CreateDirectory(downloads);

        return new SeleniumExecutionContext {
            SessionId = sessionId,
            BrowserUserDataDirectory = browser,
            ArtifactsDirectory = artifacts,
            DownloadDirectory = downloads,
            IoTempSession = io,
            FallbackTempRoot = fallback
        };
    }

    private static void EnsureOptionalDirs(params string?[] paths)
    {
        foreach (var p in paths) {
            if (!string.IsNullOrWhiteSpace(p))
                Directory.CreateDirectory(p!);
        }
    }
}
