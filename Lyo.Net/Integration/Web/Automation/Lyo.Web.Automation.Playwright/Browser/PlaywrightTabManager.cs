using System.Runtime.CompilerServices;
using Lyo.Exceptions;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using StrongBoxGuid = System.Runtime.CompilerServices.StrongBox<System.Guid>;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Page / tab operations within a Playwright <see cref="IBrowserContext" />.</summary>
public sealed class PlaywrightTabManager
{
    private static readonly ConditionalWeakTable<IPage, StrongBoxGuid> PageIds = new();
    private readonly PlaywrightBrowser _browser;
    private readonly Dictionary<Guid, string> _displayNames = [];
    private readonly ILogger _logger;

    internal PlaywrightTabManager(PlaywrightBrowser browser, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the active tab snapshot.</summary>
    public PlaywrightBrowserTabInfo GetCurrent()
        => RunTabRead(
            "get_current", () => {
                var page = _browser.GetRequiredPage();
                var ctx = _browser.GetRequiredContext();
                var pages = ctx.Pages;
                var handle = GetOrCreatePageId(page);
                var index = IndexOfPage(pages, page);
                OperationHelpers.ThrowIf(index < 0, "Current page not in context page list.");
                _displayNames.TryGetValue(handle, out var dn);
                return new PlaywrightBrowserTabInfo(index, true, handle.ToString("N"), SafeRead(() => page.Url), SafeReadTitle(page), dn);
            });

    /// <summary>Lists all pages in the context.</summary>
    public IReadOnlyList<PlaywrightBrowserTabInfo> ListTabs()
        => RunTabRead(
            "list_tabs", () => {
                var ctx = _browser.GetRequiredContext();
                var pages = ctx.Pages;
                var active = _browser.GetRequiredPage();
                var list = new List<PlaywrightBrowserTabInfo>(pages.Count);
                for (var i = 0; i < pages.Count; i++) {
                    var p = pages[i];
                    var id = GetOrCreatePageId(p);
                    _displayNames.TryGetValue(id, out var dn);
                    list.Add(new(i, ReferenceEquals(p, active), id.ToString("N"), SafeRead(() => p.Url), SafeReadTitle(p), dn));
                }

                return list;
            });

    public Task<IReadOnlyList<PlaywrightBrowserTabInfo>> ListTabsAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return ListTabs();
            }, ct);

    public Task<PlaywrightBrowserTabInfo> GetCurrentAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return GetCurrent();
            }, ct);

    /// <summary>Assigns a friendly display name for a page (keyed by <see cref="PlaywrightBrowserTabInfo.PageKey" />).</summary>
    public void SetDisplayName(string pageKey, string? displayName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentHelpers.ThrowIf(!Guid.TryParseExact(pageKey, "N", out var id), "PageKey must be a 32-character hex Guid.", nameof(pageKey));

        if (string.IsNullOrWhiteSpace(displayName))
            _displayNames.Remove(id);
        else
            _displayNames[id] = displayName!;

        _logger.LogDebug("Set display name for page {PageKey}", pageKey);
    }

    /// <summary>Switches to the page at the given 0-based index.</summary>
    public void SwitchTo(int index)
        => RunTabOp(
            "switch_index", () => {
                var ctx = _browser.GetRequiredContext();
                var pages = ctx.Pages;
                OperationHelpers.ThrowIf(pages.Count == 0, "No pages available.");
                ArgumentHelpers.ThrowIfNotInRange(index, 0, pages.Count - 1, nameof(index));
                _browser.SetActivePage(pages[index]);
            });

    public Task SwitchToAsync(int index, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                SwitchTo(index);
            }, ct);

    /// <summary>Switches to the page with the given key (<see cref="PlaywrightBrowserTabInfo.PageKey" />).</summary>
    public void SwitchTo(string pageKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentHelpers.ThrowIf(!Guid.TryParseExact(pageKey, "N", out var id), "PageKey must be a 32-character hex Guid.", nameof(pageKey));

        RunTabOp(
            "switch_key", () => {
                foreach (var p in _browser.GetRequiredContext().Pages) {
                    if (GetOrCreatePageId(p) == id) {
                        _browser.SetActivePage(p);
                        return;
                    }
                }

                throw new InvalidOperationException($"Unknown page key: {pageKey}");
            });
    }

    /// <summary>Opens a new page and optionally navigates.</summary>
    public async Task<string> OpenNewPageAsync(string? url = null, CancellationToken ct = default)
    {
        string? key = null;
        await RunTabOpAsync(
                "open_page", async () => {
                    var page = await _browser.GetRequiredContext().NewPageAsync().ConfigureAwait(false);
                    _browser.SetActivePage(page);
                    if (!string.IsNullOrWhiteSpace(url))
                        await page.GotoAsync(url!, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);

                    key = GetOrCreatePageId(page).ToString("N");
                }, ct)
            .ConfigureAwait(false);

        return key!;
    }

    /// <summary>Closes the current page and activates another if any remain.</summary>
    public async Task CloseCurrentAsync(CancellationToken ct = default)
        => await RunTabOpAsync(
                "close_current", async () => {
                    var ctx = _browser.GetRequiredContext();
                    var pages = ctx.Pages;
                    OperationHelpers.ThrowIf(pages.Count == 0, "No pages to close.");
                    var current = _browser.GetRequiredPage();
                    var id = GetOrCreatePageId(current);
                    await current.CloseAsync().ConfigureAwait(false);
                    _displayNames.Remove(id);
                    PruneDisplayNames();
                    var remaining = ctx.Pages;
                    if (remaining.Count > 0)
                        _browser.SetActivePage(remaining[0]);
                }, ct)
            .ConfigureAwait(false);

    internal void ClearDisplayNames() => _displayNames.Clear();

    private static Guid GetOrCreatePageId(IPage page)
    {
        if (PageIds.TryGetValue(page, out var box))
            return box.Value;

        var id = Guid.NewGuid();
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

    private void PruneDisplayNames()
    {
        var valid = new HashSet<Guid>();
        foreach (var p in _browser.GetRequiredContext().Pages)
            valid.Add(GetOrCreatePageId(p));

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

    private static string? SafeReadTitle(IPage page)
    {
        try {
            return page.TitleAsync().GetAwaiter().GetResult();
        }
        catch {
            return null;
        }
    }

    private T RunTabRead<T>(string operation, Func<T> func)
    {
        _logger.LogDebug("Tab read {Operation} starting", operation);
        using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation)))
            return func();
    }

    private void RunTabOp(string operation, Action action)
    {
        _logger.LogDebug("Tab operation {Operation} starting", operation);
        try {
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation)))
                action();

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "success") }));

            _logger.LogDebug("Tab operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "failure") }));

            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), ex, PlaywrightMetricTags.ForOperation(_browser, operation));
            _logger.LogWarning(ex, "Tab operation {Operation} failed", operation);
            throw;
        }
    }

    private async Task RunTabOpAsync(string operation, Func<Task> action, CancellationToken ct)
    {
        _logger.LogDebug("Tab operation {Operation} starting", operation);
        try {
            ct.ThrowIfCancellationRequested();
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation)))
                await action().ConfigureAwait(false);

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "success") }));

            _logger.LogDebug("Tab operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "failure") }));

            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), ex, PlaywrightMetricTags.ForOperation(_browser, operation));
            _logger.LogWarning(ex, "Tab operation {Operation} failed", operation);
            throw;
        }
    }
}