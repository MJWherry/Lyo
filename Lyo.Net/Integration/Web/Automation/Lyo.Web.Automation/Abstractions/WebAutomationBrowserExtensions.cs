using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>Convenience helpers for <see cref="IWebAutomationBrowser" />, <see cref="IBrowserCookies" />, and <see cref="IBrowserHeaders" />.</summary>
public static class WebAutomationBrowserExtensions
{
    /// <summary>Returns cookies via <see cref="IWebAutomationBrowser.CookieJar" />, or an empty list when not supported.</summary>
    public static Task<IReadOnlyList<BrowserCookie>> TryGetCookiesAsync(this IWebAutomationBrowser browser, string? url = null, CancellationToken ct = default)
        => browser.CookieJar?.GetCookiesAsync(url, ct) ?? Task.FromResult<IReadOnlyList<BrowserCookie>>([]);

    /// <summary>Returns session cookies formatted as a <c>Cookie</c> header value (e.g. <c>"name=value; name2=value2"</c>), or <see langword="null" /> when not supported.</summary>
    public static async Task<string?> TryGetCookieHeaderAsync(this IWebAutomationBrowser browser, string? url = null, CancellationToken ct = default)
    {
        var jar = browser.CookieJar;
        if (jar == null)
            return null;

        var cookies = await jar.GetCookiesAsync(url, ct).ConfigureAwait(false);
        return cookies.Count == 0 ? null : string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
    }

    /// <summary>Adds cookies via <see cref="IWebAutomationBrowser.CookieJar" />; no-op when not supported.</summary>
    public static Task TryAddCookiesAsync(this IWebAutomationBrowser browser, IEnumerable<BrowserCookie> cookies, CancellationToken ct = default)
        => browser.CookieJar?.AddCookiesAsync(cookies, ct) ?? Task.CompletedTask;

    /// <summary>Clears all cookies via <see cref="IWebAutomationBrowser.CookieJar" />; no-op when not supported.</summary>
    public static Task TryClearCookiesAsync(this IWebAutomationBrowser browser, CancellationToken ct = default) => browser.CookieJar?.ClearCookiesAsync(ct) ?? Task.CompletedTask;

    /// <summary>Sets extra request headers via <see cref="IWebAutomationBrowser.ExtraHeaders" />; no-op when not supported.</summary>
    public static Task TrySetExtraHeadersAsync(this IWebAutomationBrowser browser, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
        => browser.ExtraHeaders?.SetExtraHeadersAsync(headers, ct) ?? Task.CompletedTask;

    /// <summary>Clears extra request headers via <see cref="IWebAutomationBrowser.ExtraHeaders" />; no-op when not supported.</summary>
    public static Task TryClearExtraHeadersAsync(this IWebAutomationBrowser browser, CancellationToken ct = default)
        => browser.ExtraHeaders?.ClearExtraHeadersAsync(ct) ?? Task.CompletedTask;
}