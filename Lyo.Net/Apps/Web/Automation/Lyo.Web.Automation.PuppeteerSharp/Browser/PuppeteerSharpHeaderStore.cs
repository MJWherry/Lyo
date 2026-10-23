using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary><see cref="IBrowserHeaders" /> applied to every page in the session context.</summary>
internal sealed class PuppeteerSharpHeaderStore : IBrowserHeaders
{
    private readonly PuppeteerSharpBrowser _browser;

    internal PuppeteerSharpHeaderStore(PuppeteerSharpBrowser browser) => _browser = browser;

    public Task SetExtraHeadersAsync(IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(headers);
        return _browser.SetExtraHeadersAsync(headers, ct);
    }

    public Task ClearExtraHeadersAsync(CancellationToken ct = default) => _browser.SetExtraHeadersAsync(new Dictionary<string, string>(), ct);
}