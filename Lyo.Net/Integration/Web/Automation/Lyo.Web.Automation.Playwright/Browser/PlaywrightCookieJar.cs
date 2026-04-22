using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Playwright implementation of <see cref="IBrowserCookies" /> backed by <see cref="IBrowserContext" />.</summary>
internal sealed class PlaywrightCookieJar : IBrowserCookies
{
    private readonly PlaywrightBrowser _browser;

    internal PlaywrightCookieJar(PlaywrightBrowser browser) => _browser = browser;

    public async Task<IReadOnlyList<BrowserCookie>> GetCookiesAsync(string? url = null, CancellationToken ct = default)
    {
        var ctx = _browser.Context;
        if (ctx == null)
            return [];

        var urls = url != null ? new[] { url } : Array.Empty<string>();
        var cookies = await ctx.CookiesAsync(urls).ConfigureAwait(false);
        return cookies.Select(c => new BrowserCookie {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            Secure = c.Secure,
            HttpOnly = c.HttpOnly,
            Expiry = c.Expires is { } exp && exp > 0 ? DateTimeOffset.FromUnixTimeSeconds((long)exp) : null
        }).ToList<BrowserCookie>();
    }

    public async Task AddCookiesAsync(IEnumerable<BrowserCookie> cookies, CancellationToken ct = default)
    {
        var ctx = _browser.Context ?? throw new InvalidOperationException("Browser not started.");
        await ctx.AddCookiesAsync(cookies.Select(c => new Cookie {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            Secure = c.Secure,
            HttpOnly = c.HttpOnly,
            Expires = c.Expiry is { } exp ? exp.ToUnixTimeSeconds() : null
        })).ConfigureAwait(false);
    }

    public async Task ClearCookiesAsync(CancellationToken ct = default)
    {
        var ctx = _browser.Context;
        if (ctx != null)
            await ctx.ClearCookiesAsync().ConfigureAwait(false);
    }
}
