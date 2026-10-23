using Lyo.Common.Core.Extensions;
using Lyo.Web.Automation.PuppeteerSharp.Browser;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

internal static class PuppeteerSharpMetricTags
{
    public static (string, string)[] ForOperation(PuppeteerSharpBrowser browser, string operation, IEnumerable<(string, string)>? extra = null)
    {
        var list = new List<(string, string)> { ("operation", operation), ("session_id", browser.SessionIdLabel), ("implementation", "puppeteersharp") };
        var host = TryHostFromUrl(browser.TryGetCurrentUrl());
        if (!host.IsNullOrEmpty())
            list.Add(("url_host", host));

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