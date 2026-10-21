using System.Runtime.CompilerServices;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Web.Automation.PuppeteerSharp.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PuppeteerSharp;
using StrongBoxGuid = System.Runtime.CompilerServices.StrongBox<System.Guid>;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary>Page and tab operations inside a PuppeteerSharp browser context.</summary>
public sealed class PuppeteerSharpTabManager
{
    private static readonly ConditionalWeakTable<IPage, StrongBoxGuid> PageIds = new();
    private readonly PuppeteerSharpBrowser _browser;
    private readonly Dictionary<Guid, string> _displayNames = [];
    private readonly ILogger _logger;

    internal PuppeteerSharpTabManager(PuppeteerSharpBrowser browser, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Returns the active tab snapshot.</summary>
    public Task<PuppeteerSharpBrowserTabInfo> GetCurrentAsync(CancellationToken ct = default)
        => RunTabReadAsync(
            "get_current", async () => {
                var page = _browser.GetRequiredPage();
                var pages = await _browser.GetRequiredContext().PagesAsync().ConfigureAwait(false);
                var handle = GetOrCreatePageId(page);
                var index = IndexOfPage(pages, page);
                OperationHelpers.ThrowIf(index < 0, "Current page not in context page list.");
                _displayNames.TryGetValue(handle, out var dn);
                return new PuppeteerSharpBrowserTabInfo(index, true, handle.ToString("N"), SafeRead(() => page.Url), await SafeTitleAsync(page).ConfigureAwait(false), dn);
            }, ct);

    /// <summary>Lists every page in the context.</summary>
    public Task<IReadOnlyList<PuppeteerSharpBrowserTabInfo>> ListTabsAsync(CancellationToken ct = default)
        => RunTabReadAsync(
            "list_tabs", async () => {
                var pages = await _browser.GetRequiredContext().PagesAsync().ConfigureAwait(false);
                var active = _browser.GetRequiredPage();
                var list = new List<PuppeteerSharpBrowserTabInfo>(pages.Length);
                for (var i = 0; i < pages.Length; i++) {
                    var page = pages[i];
                    var id = GetOrCreatePageId(page);
                    _displayNames.TryGetValue(id, out var dn);
                    list.Add(new(i, ReferenceEquals(page, active), id.ToString("N"), SafeRead(() => page.Url), await SafeTitleAsync(page).ConfigureAwait(false), dn));
                }

                return (IReadOnlyList<PuppeteerSharpBrowserTabInfo>)list;
            }, ct);

    /// <summary>Assigns a friendly display name keyed on <see cref="PuppeteerSharpBrowserTabInfo.PageKey" />.</summary>
    public Task SetDisplayNameAsync(string pageKey, string? displayName, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pageKey);
        ct.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIf(!Guid.TryParseExact(pageKey, "N", out var id), "PageKey must be a 32-character hex Guid.", nameof(pageKey));
        if (displayName.IsNullOrWhitespace())
            _displayNames.Remove(id);
        else
            _displayNames[id] = displayName;

        _logger.LogDebug("Set display name for page {PageKey}", pageKey);
        return Task.CompletedTask;
    }

    /// <summary>Switches to the page at the given zero-based index.</summary>
    public Task SwitchToAsync(int index, CancellationToken ct = default)
        => RunTabOpAsync(
            "switch_index", async () => {
                var pages = await _browser.GetRequiredContext().PagesAsync().ConfigureAwait(false);
                OperationHelpers.ThrowIf(pages.Length == 0, "No pages available.");
                ArgumentHelpers.ThrowIfNotInRange(index, 0, pages.Length - 1);
                var page = pages[index];
                await page.BringToFrontAsync().ConfigureAwait(false);
                _browser.SetActivePage(page);
            }, ct);

    /// <summary>Switches to the page that has the given key.</summary>
    public Task SwitchToAsync(string pageKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentHelpers.ThrowIf(!Guid.TryParseExact(pageKey, "N", out var id), "PageKey must be a 32-character hex Guid.", nameof(pageKey));
        return RunTabOpAsync(
            "switch_key", async () => {
                foreach (var page in await _browser.GetRequiredContext().PagesAsync().ConfigureAwait(false)) {
                    if (GetOrCreatePageId(page) != id)
                        continue;

                    await page.BringToFrontAsync().ConfigureAwait(false);
                    _browser.SetActivePage(page);
                    return;
                }

                throw new InvalidOperationException($"Unknown page key: {pageKey}");
            }, ct);
    }

    /// <summary>Opens a new page and optionally navigates to a URL.</summary>
    public async Task<string> OpenNewPageAsync(string? url = null, CancellationToken ct = default)
    {
        string? key = null;
        await RunTabOpAsync(
                "open_page", async () => {
                    var page = await _browser.GetRequiredContext().NewPageAsync().ConfigureAwait(false);
                    await _browser.ApplyPageDefaultsAsync(page).ConfigureAwait(false);
                    _browser.SetActivePage(page);
                    if (!url.IsNullOrWhitespace())
                        await page.GoToAsync(url, _browser.NavigationOptions(ct)).ConfigureAwait(false);

                    key = GetOrCreatePageId(page).ToString("N");
                }, ct)
            .ConfigureAwait(false);

        return key!;
    }

    /// <summary>Closes the current page and activates another when any remain.</summary>
    public Task CloseCurrentAsync(CancellationToken ct = default)
        => RunTabOpAsync(
            "close_current", async () => {
                var context = _browser.GetRequiredContext();
                var pages = await context.PagesAsync().ConfigureAwait(false);
                OperationHelpers.ThrowIf(pages.Length == 0, "No pages to close.");
                var current = _browser.GetRequiredPage();
                var id = GetOrCreatePageId(current);
                await current.CloseAsync().ConfigureAwait(false);
                _displayNames.Remove(id);
                var remaining = await context.PagesAsync().ConfigureAwait(false);
                PruneDisplayNames(remaining);
                if (remaining.Length > 0) {
                    await remaining[0].BringToFrontAsync().ConfigureAwait(false);
                    _browser.SetActivePage(remaining[0]);
                }
            }, ct);

    internal void ClearDisplayNames() => _displayNames.Clear();

    private static Guid GetOrCreatePageId(IPage page)
    {
        if (PageIds.TryGetValue(page, out var box))
            return box.Value;

        var id = LyoGuid.CreateV7();
        PageIds.Add(page, new(id));
        return id;
    }

    private static int IndexOfPage(IReadOnlyList<IPage> pages, IPage page)
    {
        for (var i = 0; i < pages.Count; i++) {
            if (ReferenceEquals(pages[i], page))
                return i;
        }

        return -1;
    }

    private void PruneDisplayNames(IEnumerable<IPage> pages)
    {
        var valid = new HashSet<Guid>();
        foreach (var page in pages)
            valid.Add(GetOrCreatePageId(page));

        foreach (var key in _displayNames.Keys.ToList()) {
            if (!valid.Contains(key))
                _displayNames.Remove(key);
        }
    }

    private static string? SafeRead(Func<string> read)
    {
        try {
            return read();
        }
        catch {
            return null;
        }
    }

    private static async Task<string?> SafeTitleAsync(IPage page)
    {
        try {
            return await page.GetTitleAsync().ConfigureAwait(false);
        }
        catch {
            return null;
        }
    }

    private async Task<T> RunTabReadAsync<T>(string operation, Func<Task<T>> func, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Tab read {Operation} starting", operation);
        using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), PuppeteerSharpMetricTags.ForOperation(_browser, operation)))
            return await func().ConfigureAwait(false);
    }

    private async Task RunTabOpAsync(string operation, Func<Task> action, CancellationToken ct)
    {
        _logger.LogDebug("Tab operation {Operation} starting", operation);
        try {
            ct.ThrowIfCancellationRequested();
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), PuppeteerSharpMetricTags.ForOperation(_browser, operation)))
                await action().ConfigureAwait(false);

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "success")]));

            _logger.LogDebug("Tab operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "failure")]));

            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), ex, PuppeteerSharpMetricTags.ForOperation(_browser, operation));
            _logger.LogWarning(ex, "Tab operation {Operation} failed", operation);
            throw;
        }
    }
}