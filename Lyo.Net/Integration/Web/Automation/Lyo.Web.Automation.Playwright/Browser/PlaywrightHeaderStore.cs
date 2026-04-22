using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Playwright implementation of <see cref="IBrowserHeaders" /> backed by <see cref="Microsoft.Playwright.IBrowserContext" />.</summary>
internal sealed class PlaywrightHeaderStore : IBrowserHeaders
{
    private readonly PlaywrightBrowser _browser;

    internal PlaywrightHeaderStore(PlaywrightBrowser browser) => _browser = browser;

    public async Task SetExtraHeadersAsync(IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        var ctx = _browser.Context ?? throw new InvalidOperationException("Browser not started.");
        await ctx.SetExtraHTTPHeadersAsync(headers).ConfigureAwait(false);
    }

    public async Task ClearExtraHeadersAsync(CancellationToken ct = default)
    {
        var ctx = _browser.Context;
        if (ctx != null)
            await ctx.SetExtraHTTPHeadersAsync(new Dictionary<string, string>()).ConfigureAwait(false);
    }
}
