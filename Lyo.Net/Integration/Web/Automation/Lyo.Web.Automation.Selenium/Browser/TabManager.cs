using Lyo.Exceptions;
using Lyo.Web.Automation.Selenium.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenQA.Selenium;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Tab/window operations and metadata. Obtain via <see cref="SeleniumBrowser.Tabs" /> or <see cref="ISeleniumBrowserSession.Tabs" />.</summary>
public sealed class TabManager
{
    private readonly Dictionary<string, string> _displayNames = new(StringComparer.Ordinal);
    private readonly ILogger _logger;
    private readonly SeleniumBrowser _scraper;

    internal TabManager(SeleniumBrowser scraper, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(scraper);
        _scraper = scraper;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the active tab snapshot (title, URL, display name, index).</summary>
    public BrowserTabInfo GetCurrent()
        => RunTabRead<BrowserTabInfo>(
            "get_current", () => {
                var driver = _scraper.GetRequiredDriver();
                var handle = driver.CurrentWindowHandle;
                var handles = driver.WindowHandles.ToList();
                var index = handles.IndexOf(handle);
                OperationHelpers.ThrowIf(index < 0, "Current window handle not in tab list.");
                _displayNames.TryGetValue(handle, out var dn);
                return new(index, true, handle, SafeRead(() => driver.Url), SafeRead(() => driver.Title), dn);
            });

    /// <summary>Lists all tabs with URL/title (briefly switches through each tab to read metadata, then restores the original tab).</summary>
    public IReadOnlyList<BrowserTabInfo> ListTabs()
        => RunTabRead<IReadOnlyList<BrowserTabInfo>>(
            "list_tabs", () => {
                var driver = _scraper.GetRequiredDriver();
                var handles = driver.WindowHandles.ToList();
                if (handles.Count == 0)
                    return Array.Empty<BrowserTabInfo>();

                var original = driver.CurrentWindowHandle;
                var list = new List<BrowserTabInfo>(handles.Count);
                try {
                    for (var i = 0; i < handles.Count; i++) {
                        var h = handles[i];
                        driver.SwitchTo().Window(h);
                        _displayNames.TryGetValue(h, out var dn);
                        list.Add(new(i, h == original, h, SafeRead(() => driver.Url), SafeRead(() => driver.Title), dn));
                    }
                }
                finally {
                    driver.SwitchTo().Window(original);
                }

                return list;
            });

    /// <summary>Async variant of <see cref="ListTabs" />.</summary>
    public Task<IReadOnlyList<BrowserTabInfo>> ListTabsAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return ListTabs();
            }, ct);

    /// <summary>Async variant of <see cref="GetCurrent" />.</summary>
    public Task<BrowserTabInfo> GetCurrentAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return GetCurrent();
            }, ct);

    /// <summary>Assigns a friendly display name for a tab (keyed by window handle).</summary>
    public void SetDisplayName(string windowHandle, string? displayName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(windowHandle);
        if (string.IsNullOrWhiteSpace(displayName))
            _displayNames.Remove(windowHandle);
        else
            _displayNames[windowHandle] = displayName!;

        _logger.LogDebug("Set display name for tab {Handle}", windowHandle);
    }

    /// <summary>Switches to the tab at the given 0-based index.</summary>
    public void SwitchTo(int index)
        => RunTabOp(
            "switch_index", () => {
                var driver = _scraper.GetRequiredDriver();
                var handles = driver.WindowHandles.ToList();
                OperationHelpers.ThrowIf(handles.Count == 0, "No browser tabs available.");
                ArgumentHelpers.ThrowIfNotInRange(index, 0, handles.Count - 1);
                driver.SwitchTo().Window(handles[index]);
            });

    /// <summary>Async variant of <see cref="SwitchTo(int)" />.</summary>
    public Task SwitchToAsync(int index, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                SwitchTo(index);
            }, ct);

    /// <summary>Switches to the tab with the given window handle.</summary>
    public void SwitchTo(string windowHandle)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(windowHandle);
        RunTabOp("switch_handle", () => _scraper.GetRequiredDriver().SwitchTo().Window(windowHandle));
    }

    /// <summary>Switches to the first tab whose <see cref="BrowserTabInfo.DisplayName" /> matches using ordinal comparison.</summary>
    public void SwitchToDisplayName(string displayName) => SwitchToDisplayName(displayName, StringComparison.Ordinal);

    /// <summary>Switches to the first tab whose <see cref="BrowserTabInfo.DisplayName" /> matches.</summary>
    public void SwitchToDisplayName(string displayName, StringComparison comparison)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(displayName);
        RunTabOp(
            "switch_display_name", () => {
                foreach (var kv in _displayNames) {
                    if (string.Equals(kv.Value, displayName, comparison)) {
                        _scraper.GetRequiredDriver().SwitchTo().Window(kv.Key);
                        return;
                    }
                }

                throw new InvalidOperationException($"No tab with display name '{displayName}'.");
            });
    }

    /// <summary>Switches to the first tab that matches <paramref name="predicate" /> (uses <see cref="ListTabs" />).</summary>
    public bool TrySwitchTo(Predicate<BrowserTabInfo> predicate, out BrowserTabInfo? tab)
    {
        ArgumentHelpers.ThrowIfNull(predicate);
        tab = null;
        foreach (var t in ListTabs()) {
            if (!predicate(t))
                continue;

            RunTabOp("switch_predicate", () => _scraper.GetRequiredDriver().SwitchTo().Window(t.WindowHandle));
            tab = t with { IsActive = true };
            return true;
        }

        return false;
    }

    /// <summary>Closes every tab except <paramref name="windowHandleToKeep" />.</summary>
    public void CloseAllExcept(string windowHandleToKeep)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(windowHandleToKeep);
        RunTabOp(
            "close_all_except", () => {
                while (true) {
                    var hs = _scraper.GetRequiredDriver().WindowHandles.ToList();
                    var victim = hs.FirstOrDefault(h => h != windowHandleToKeep);
                    if (victim == null)
                        break;

                    CloseSingleTabByHandle(victim);
                }
            });
    }

    /// <summary>Closes every tab except the currently active one.</summary>
    public void CloseAllExceptCurrent()
        => RunTabOp(
            "close_all_except_current", () => {
                var keep = _scraper.GetRequiredDriver().CurrentWindowHandle;
                while (true) {
                    var hs = _scraper.GetRequiredDriver().WindowHandles.ToList();
                    var victim = hs.FirstOrDefault(h => h != keep);
                    if (victim == null)
                        break;

                    CloseSingleTabByHandle(victim);
                }
            });

    /// <summary>Opens a new tab. Optionally navigates to a URL. Returns the new window handle.</summary>
    public string OpenNewTab(string? url = null)
    {
        string? result = null;
        RunTabOp("open_tab_js", () => result = OpenNewTabJsCore(url));
        return result!;
    }

    /// <summary>Opens a new tab using Selenium 4 <c>NewWindow(Tab)</c>; falls back to <see cref="OpenNewTab" /> on failure.</summary>
    public string OpenNewTabNative()
    {
        string? result = null;
        RunTabOp(
            "open_tab_native", () => {
                var driver = _scraper.GetRequiredDriver();
                var before = new HashSet<string>(driver.WindowHandles, StringComparer.Ordinal);
                try {
                    driver.SwitchTo().NewWindow(WindowType.Tab);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "NewWindow(Tab) failed; using window.open");
                    result = OpenNewTabJsCore("about:blank");
                    return;
                }

                var after = driver.WindowHandles.ToList();
                var newHandle = after.FirstOrDefault(h => !before.Contains(h));
                result = newHandle != null ? OpenNewTabHandle(driver, newHandle) : OpenNewTabJsCore("about:blank");
            });

        return result!;
    }

    /// <summary>Opens a new browser window (not a tab) using Selenium 4 <c>NewWindow(Window)</c>, with JS fallback.</summary>
    public string OpenNewWindow(string? url = null)
    {
        string? result = null;
        RunTabOp(
            "open_window_native", () => {
                var driver = _scraper.GetRequiredDriver();
                var before = new HashSet<string>(driver.WindowHandles, StringComparer.Ordinal);
                try {
                    driver.SwitchTo().NewWindow(WindowType.Window);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "NewWindow(Window) failed; using window.open with features");
                    OpenNewWindowFallback(driver, url);
                    result = driver.CurrentWindowHandle;
                    return;
                }

                var after = driver.WindowHandles.ToList();
                var newHandle = after.FirstOrDefault(h => !before.Contains(h));
                if (newHandle == null) {
                    OpenNewWindowFallback(driver, url);
                    result = driver.CurrentWindowHandle;
                    return;
                }

                result = OpenNewTabHandle(driver, newHandle);
                if (!string.IsNullOrWhiteSpace(url))
                    _scraper.NavigateTo(url!);
            });

        return result!;
    }

    /// <summary>Opens a new tab, optionally waits for <c>document.readyState === 'complete'</c>.</summary>
    public async Task<string> OpenNewTabAndWaitForLoadAsync(string? url = null, CancellationToken ct = default)
    {
        var handle = await Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    return string.IsNullOrWhiteSpace(url) ? OpenNewTabNative() : OpenNewTab(url);
                }, ct)
            .ConfigureAwait(false);

        await _scraper.WaitForDocumentReadyAsync(ct).ConfigureAwait(false);
        return handle;
    }

    /// <summary>Closes the current tab and switches to another if any remain.</summary>
    public void CloseCurrent()
        => RunTabOp(
            "close_current", () => {
                var driver = _scraper.GetRequiredDriver();
                var handle = driver.CurrentWindowHandle;
                var handles = driver.WindowHandles.ToList();
                OperationHelpers.ThrowIf(handles.Count == 0, "No tabs to close.");
                driver.Close();
                _displayNames.Remove(handle);
                PruneDisplayNames(driver.WindowHandles);
                var remaining = driver.WindowHandles;
                if (remaining.Count > 0)
                    driver.SwitchTo().Window(remaining[0]);
            });

    /// <summary>Closes the tab at the given index.</summary>
    public void Close(int index)
        => RunTabOp(
            "close_index", () => {
                var driver = _scraper.GetRequiredDriver();
                var handles = driver.WindowHandles.ToList();
                OperationHelpers.ThrowIf(handles.Count == 0, "No tabs to close.");
                ArgumentHelpers.ThrowIfNotInRange(index, 0, handles.Count - 1);
                CloseSingleTabByHandle(handles[index]);
            });

    /// <summary>Closes the tab with the given window handle.</summary>
    public void Close(string windowHandle)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(windowHandle);
        RunTabOp(
            "close_handle", () => {
                var driver = _scraper.GetRequiredDriver();
                var handles = driver.WindowHandles.ToList();
                OperationHelpers.ThrowIf(handles.IndexOf(windowHandle) < 0, "Unknown window handle.");
                CloseSingleTabByHandle(windowHandle);
            });
    }

    /// <summary>Raw window handles in tab bar order (current driver API).</summary>
    public IReadOnlyList<string> GetWindowHandles() => RunTabRead<IReadOnlyList<string>>("get_window_handles", () => _scraper.GetRequiredDriver().WindowHandles.ToList());

    internal void ClearDisplayNames() => _displayNames.Clear();

    private static string OpenNewTabHandle(IWebDriver driver, string newHandle)
    {
        driver.SwitchTo().Window(newHandle);
        return newHandle;
    }

    private string OpenNewTabJsCore(string? url)
    {
        var driver = _scraper.GetRequiredDriver();
        var before = new HashSet<string>(driver.WindowHandles, StringComparer.Ordinal);
        var openUrl = string.IsNullOrWhiteSpace(url) ? "about:blank" : url!;
        ((IJavaScriptExecutor)driver).ExecuteScript("window.open(arguments[0], '_blank');", openUrl);
        var after = driver.WindowHandles.ToList();
        var newHandle = after.FirstOrDefault(h => !before.Contains(h));
        ArgumentHelpers.ThrowIfNull(newHandle);
        driver.SwitchTo().Window(newHandle);
        return newHandle;
    }

    private void CloseSingleTabByHandle(string targetHandle)
    {
        var driver = _scraper.GetRequiredDriver();
        var handles = driver.WindowHandles.ToList();
        if (handles.IndexOf(targetHandle) < 0)
            return;

        var original = driver.CurrentWindowHandle;
        driver.SwitchTo().Window(targetHandle);
        driver.Close();
        _displayNames.Remove(targetHandle);
        PruneDisplayNames(driver.WindowHandles);
        var remaining = driver.WindowHandles;
        if (remaining.Count > 0) {
            var fallback = remaining.Contains(original) ? original : remaining[0];
            driver.SwitchTo().Window(fallback);
        }
    }

    private void OpenNewWindowFallback(IWebDriver driver, string? url)
    {
        var openUrl = string.IsNullOrWhiteSpace(url) ? "about:blank" : url!;
        ((IJavaScriptExecutor)driver).ExecuteScript("window.open(arguments[0], '_blank', 'popup,width=800,height=600');", openUrl);
        var handles = driver.WindowHandles.ToList();
        if (handles.Count > 0)
            driver.SwitchTo().Window(handles[handles.Count - 1]);
    }

    private T RunTabRead<T>(string operation, Func<T> func)
    {
        _logger.LogDebug("Tab read {Operation} starting", operation);
        using (_scraper.Metrics.StartTimer(_scraper.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), SeleniumMetricTags.ForOperation(_scraper, operation)))
            return func();
    }

    private void RunTabOp(string operation, Action action)
    {
        _logger.LogDebug("Tab operation {Operation} starting", operation);
        try {
            using (_scraper.Metrics.StartTimer(_scraper.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), SeleniumMetricTags.ForOperation(_scraper, operation)))
                action();

            _scraper.Metrics.IncrementCounter(
                _scraper.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: SeleniumMetricTags.ForOperation(_scraper, operation, new[] { ("result", "success") }));

            _logger.LogDebug("Tab operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _scraper.Metrics.IncrementCounter(
                _scraper.ResolveMetric(nameof(Wm.Metrics.TabOperation)), tags: SeleniumMetricTags.ForOperation(_scraper, operation, new[] { ("result", "failure") }));

            _scraper.Metrics.RecordError(_scraper.ResolveMetric(nameof(Wm.Metrics.TabOperationDuration)), ex, SeleniumMetricTags.ForOperation(_scraper, operation));
            _logger.LogWarning(ex, "Tab operation {Operation} failed", operation);
            throw;
        }
    }

    private void PruneDisplayNames(IReadOnlyCollection<string> validHandles)
    {
        var set = new HashSet<string>(validHandles, StringComparer.Ordinal);
        foreach (var key in _displayNames.Keys.ToList()) {
            if (!set.Contains(key))
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
}