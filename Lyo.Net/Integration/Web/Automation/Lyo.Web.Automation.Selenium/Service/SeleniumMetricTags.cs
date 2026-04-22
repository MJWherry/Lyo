using Lyo.Web.Automation.Selenium.Browser;

namespace Lyo.Web.Automation.Selenium.Service;

internal static class SeleniumMetricTags
{
    public static (string, string)[] ForOperation(SeleniumBrowser scraper, string operation, IEnumerable<(string, string)>? extra = null)
    {
        var list = new List<(string, string)> { ("operation", operation), ("session_id", scraper.SessionIdLabel), ("implementation", "selenium") };
        var host = TryHostFromUrl(scraper.TryGetCurrentUrl());
        if (!string.IsNullOrEmpty(host))
            list.Add(("url_host", host!));

        if (extra != null)
            list.AddRange(extra);

        return list.ToArray();
    }

    public static (string, string)[] ForOperation(SeleniumBrowser scraper, string operation, string? urlForHost, IEnumerable<(string, string)>? extra = null)
    {
        var list = new List<(string, string)> { ("operation", operation), ("session_id", scraper.SessionIdLabel), ("implementation", "selenium") };
        var host = TryHostFromUrl(urlForHost) ?? TryHostFromUrl(scraper.TryGetCurrentUrl());
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
