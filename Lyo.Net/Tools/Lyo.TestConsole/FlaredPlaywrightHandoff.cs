using Lyo.Exceptions;
using Lyo.Http.Client;
using Lyo.Http.Client.Flared;
using Lyo.Http.Client.Session;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Configuration;

namespace Lyo.TestConsole;

/// <summary>
/// Host-owned Flared → Playwright glue. Playwright does not reference <c>Lyo.Http.Client</c>; copy UA and cookies here after a solver round-trip.
/// FlareSolverr is Chromium — pin UA only when <see cref="PlaywrightBrowserKind.Chromium" />. <c>cf_clearance</c> is IP-bound: set the same
/// <see cref="LyoHttpClientOptions.ProxyUrl" /> on Flared and Playwright.
/// </summary>
internal static class FlaredPlaywrightHandoff
{
    /// <summary>Pins solver UA (Chromium only) and copies Flared's proxy onto Playwright so clearance stays same-IP.</summary>
    public static void Apply(PlaywrightBrowserOptions options, ILyoHttpSession session, FlaredHttpOptions? flared = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(session);
        OperationHelpers.ThrowIf(
            options.BrowserKind != PlaywrightBrowserKind.Chromium,
            "Flared cookie handoff requires PlaywrightBrowserKind.Chromium (FlareSolverr). Firefox/WebKit cannot use solver clearance cookies.");
        if (!string.IsNullOrWhiteSpace(session.UserAgent))
            options.UserAgents = [session.UserAgent];
        if (flared is null || string.IsNullOrWhiteSpace(flared.ProxyUrl))
            return;

        options.ProxyUrl = flared.ProxyUrl;
        options.ProxyUsername = flared.ProxyUsername;
        options.ProxyPassword = flared.ProxyPassword;
    }

    /// <summary>Writes HTTP-session cookies into the Playwright context. Call after <c>StartBrowserAsync</c> and before navigate.</summary>
    public static Task ApplyCookiesAsync(IWebAutomationBrowser browser, ILyoHttpSession session, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        ArgumentHelpers.ThrowIfNull(session);
        var jar = browser.CookieJar;
        OperationHelpers.ThrowIfNull(jar, "Browser not started. Call StartBrowserAsync before ApplyCookiesAsync.");
        return jar.AddCookiesAsync(
            session.CookieJar.Export()
                .Select(c => new BrowserCookie {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Secure = c.Secure,
                    HttpOnly = c.HttpOnly,
                    Expiry = c.Expiry
                })
                .ToList(),
            ct);
    }
}
