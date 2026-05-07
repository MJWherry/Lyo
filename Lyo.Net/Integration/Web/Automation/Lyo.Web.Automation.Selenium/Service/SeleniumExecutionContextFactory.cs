using Lyo.Web.Automation.Selenium.Configuration;

namespace Lyo.Web.Automation.Selenium.Service;

internal static class SeleniumExecutionContextFactory
{
    public static SeleniumExecutionContext Create(SeleniumBrowserOptions options, Guid sessionId)
    {
        var serviceRoot = Path.GetFullPath(options.ServiceRootDirectory);
        var sessionDir = Path.Combine(serviceRoot, $"session-{sessionId:N}");
        Directory.CreateDirectory(sessionDir);

        string OrSub(string? explicitPath, string defaultSegment) => !string.IsNullOrWhiteSpace(explicitPath) ? explicitPath : Path.Combine(sessionDir, defaultSegment);

        var browser = OrSub(options.BrowserUserDataDirectory, "browser-profile");
        var artifacts = OrSub(options.ArtifactsDirectory, "artifacts");
        var downloads = OrSub(options.DownloadDirectory, "downloads");
        Directory.CreateDirectory(browser);
        Directory.CreateDirectory(artifacts);
        Directory.CreateDirectory(downloads);
        return new() {
            SessionId = sessionId,
            SessionDirectory = sessionDir,
            BrowserUserDataDirectory = browser,
            ArtifactsDirectory = artifacts,
            DownloadDirectory = downloads,
            LoggerProvider = new(sessionDir)
        };
    }
}