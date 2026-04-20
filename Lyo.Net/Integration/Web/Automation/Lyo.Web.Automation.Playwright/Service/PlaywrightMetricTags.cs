using Lyo.Web.Automation.Playwright.Browser;

namespace Lyo.Web.Automation.Playwright.Service;

internal static class PlaywrightMetricTags
{
    public static (string, string)[] ForOperation(PlaywrightBrowser browser, string operation, IEnumerable<(string, string)>? extra = null)
    {
        var list = new List<(string, string)> { ("operation", operation), ("session_id", browser.SessionIdLabel), ("implementation", "playwright") };
        var host = TryHostFromUrl(browser.TryGetCurrentUrl());
        if (!string.IsNullOrEmpty(host))
            list.Add(("url_host", host!));

        if (extra != null)
            list.AddRange(extra);

        return list.ToArray();
    }

    private static string? TryHostFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var u))
            return null;

        return string.IsNullOrWhiteSpace(u.Host) ? null : u.Host;
    }
}
