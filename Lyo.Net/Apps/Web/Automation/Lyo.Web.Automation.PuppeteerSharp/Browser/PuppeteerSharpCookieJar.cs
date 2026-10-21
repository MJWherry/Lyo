using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using PuppeteerSharp;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary><see cref="IBrowserCookies" /> over the PuppeteerSharp browser context.</summary>
internal sealed class PuppeteerSharpCookieJar : IBrowserCookies
{
    private readonly PuppeteerSharpBrowser _browser;

    internal PuppeteerSharpCookieJar(PuppeteerSharpBrowser browser) => _browser = browser;

    public async Task<IReadOnlyList<BrowserCookie>> GetCookiesAsync(string? url = null, CancellationToken ct = default)
    {
        var context = _browser.Context;
        if (context == null)
            return [];

        ct.ThrowIfCancellationRequested();
        var cookies = string.IsNullOrWhiteSpace(url)
            ? await context.GetCookiesAsync().ConfigureAwait(false)
            : await _browser.GetRequiredPage().GetCookiesAsync(url).ConfigureAwait(false);

        return cookies.Select(c => new BrowserCookie {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Secure = c.Secure,
                HttpOnly = c.HttpOnly,
                Expiry = c.Expires is > 0 ? DateTimeOffset.FromUnixTimeSeconds((long)c.Expires) : null
            })
            .ToList();
    }

    public async Task AddCookiesAsync(IEnumerable<BrowserCookie> cookies, CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfNull(_browser.Context, "Browser not started.");
        ct.ThrowIfCancellationRequested();
        var payload = cookies.Select(c => new CookieData {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Secure = c.Secure ?? false,
                HttpOnly = c.HttpOnly ?? false,
                Expires = c.Expiry is { } exp ? exp.ToUnixTimeSeconds() : null
            })
            .ToArray();

        await _browser.Context!.SetCookieAsync(payload).ConfigureAwait(false);
    }

    public async Task ClearCookiesAsync(CancellationToken ct = default)
    {
        var context = _browser.Context;
        if (context == null)
            return;

        ct.ThrowIfCancellationRequested();
        var cookies = await context.GetCookiesAsync().ConfigureAwait(false);
        if (cookies.Length == 0)
            return;

        var delete = cookies.Select(c => new CookieParam { Name = c.Name, Domain = c.Domain, Path = c.Path }).ToArray();
        await context.DeleteCookieAsync(delete).ConfigureAwait(false);
    }
}