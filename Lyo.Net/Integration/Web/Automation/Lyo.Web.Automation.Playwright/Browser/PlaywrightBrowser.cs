using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Wm = Lyo.Web.Automation.Core.Constants;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Playwright session façade: tabs, frames, dialogs, keyboard, metrics, and <see cref="IWebAutomationBrowser" />.</summary>
public sealed class PlaywrightBrowser : IWebAutomationBrowser, IDisposable, IAsyncDisposable
{
    private readonly PlaywrightExecutionContext? _executionContext;
    private readonly PlaywrightBrowserOptions _options;
    private readonly ILogger<PlaywrightBrowser> _logger;
    private readonly IMetrics _metrics;
    private readonly Dictionary<string, string> _metricNames;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _ownsPlaywrightStack;
    private bool _disposed;

    private PlaywrightTabManager? _tabs;
    private PlaywrightFrameNavigator? _frames;
    private PlaywrightDialogs? _dialogs;
    private PlaywrightKeyboard? _keyboard;
    private PlaywrightCookieJar? _cookieJar;
    private PlaywrightHeaderStore? _headerStore;

    /// <summary>Creates a browser that will be launched via <see cref="StartBrowserAsync" />.</summary>
    public PlaywrightBrowser(
        PlaywrightBrowserOptions options,
        PlaywrightExecutionContext? executionContext = null,
        ILogger<PlaywrightBrowser>? logger = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        _options = options;
        _executionContext = executionContext ?? PlaywrightExecutionContextFactory.Create(options, Guid.NewGuid());
        var baseLogger = logger ?? NullLogger<PlaywrightBrowser>.Instance;
        _logger = _executionContext.BuildLogger(baseLogger);
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Attaches to an existing page (does not own Playwright resources unless <see cref="PlaywrightBrowserOptions.CloseOwnedResourcesOnDispose" /> is set).</summary>
    public PlaywrightBrowser(IPage page, PlaywrightBrowserOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(page, nameof(page));
        _options = options ?? new PlaywrightBrowserOptions();
        _logger = NullLogger<PlaywrightBrowser>.Instance;
        _metrics = NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
        _page = page;
        _context = page.Context;
        _ownsPlaywrightStack = false;
        ApplyDefaultTimeouts(_page);
    }

    /// <summary>Correlation id when created from <see cref="IPlaywrightBrowserService.CreateSession" />; otherwise <see cref="Guid.Empty" />.</summary>
    public Guid SessionId => _executionContext?.SessionId ?? Guid.Empty;

    /// <summary>Session-scoped paths when created from <see cref="IPlaywrightBrowserService.CreateSession" />.</summary>
    public PlaywrightExecutionContext? ExecutionContext => _executionContext;

    /// <summary>Configuration used for this session.</summary>
    internal PlaywrightBrowserOptions Options => _options;

    internal ILogger<PlaywrightBrowser> Logger => _logger;

    internal IMetrics Metrics => _metrics;

    internal string SessionIdLabel => SessionId == Guid.Empty ? "none" : SessionId.ToString("N");

    /// <summary>Resolved metric name for members of <see cref="Constants.Metrics" /> (Playwright-specific series).</summary>
    internal string ResolveMetric(string metricMemberName) => _metricNames[metricMemberName];

    /// <summary>Underlying Playwright page for the active tab.</summary>
    public IPage? Page => _page;

    /// <summary>Underlying browser context when started.</summary>
    public IBrowserContext? Context => _context;

    /// <inheritdoc />
    public IBrowserCookies? CookieJar => _context != null ? _cookieJar ??= new PlaywrightCookieJar(this) : null;

    /// <inheritdoc />
    public IBrowserHeaders? ExtraHeaders => _context != null ? _headerStore ??= new PlaywrightHeaderStore(this) : null;

    /// <summary>Tab / page management.</summary>
    public PlaywrightTabManager Tabs => _tabs ??= new PlaywrightTabManager(this, _logger);

    /// <summary>Nested iframe navigation.</summary>
    public PlaywrightFrameNavigator Frames => _frames ??= new PlaywrightFrameNavigator(this);

    /// <summary>Dialog helpers.</summary>
    public PlaywrightDialogs Dialogs => _dialogs ??= new PlaywrightDialogs(this);

    /// <summary>Keyboard input.</summary>
    public PlaywrightKeyboard Keyboard => _keyboard ??= new PlaywrightKeyboard(this);

    /// <summary>Begins a structured logging scope (<c>session_id</c>, <c>operation</c>, <c>url</c>).</summary>
    public IDisposable? BeginOperationScope(string operation, string? urlOverride = null)
    {
        var url = urlOverride ?? TryGetCurrentUrl();
        return _logger.BeginScope(
            new Dictionary<string, object?> {
                ["session_id"] = SessionIdLabel,
                ["operation"] = operation,
                ["url"] = url == null ? null : BrowserUrlRedaction.ForLog(url, _options.MaskSensitiveUrlsInLogs)
            });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _tabs?.ClearDisplayNames();
        _tabs = null;
        _frames = null;
        _dialogs = null;
        _keyboard = null;
        _cookieJar = null;
        _headerStore = null;
        if (_ownsPlaywrightStack && _options.CloseOwnedResourcesOnDispose) {
            try {
                _context?.CloseAsync().GetAwaiter().GetResult();
            }
            catch {
                // best-effort
            }

            try {
                _browser?.CloseAsync().GetAwaiter().GetResult();
            }
            catch {
                // best-effort
            }

            _playwright?.Dispose();
        }

        _context = null;
        _browser = null;
        _page = null;
        _playwright = null;
        _executionContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _tabs?.ClearDisplayNames();
        _tabs = null;
        _frames = null;
        _dialogs = null;
        _keyboard = null;
        _cookieJar = null;
        _headerStore = null;
        if (_ownsPlaywrightStack && _options.CloseOwnedResourcesOnDispose) {
            try {
                if (_context != null)
                    await _context.CloseAsync().ConfigureAwait(false);
            }
            catch {
                // best-effort
            }

            try {
                if (_browser != null)
                    await _browser.CloseAsync().ConfigureAwait(false);
            }
            catch {
                // best-effort
            }

            _playwright?.Dispose();
        }

        _context = null;
        _browser = null;
        _page = null;
        _playwright = null;
        await (_executionContext?.DisposeAsync() ?? default).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Launches the browser, context, and initial page.</summary>
    public async Task StartBrowserAsync(CancellationToken ct = default)
    {
        if (_page != null && _ownsPlaywrightStack) {
            _logger.LogDebug("Browser already started");
            return;
        }

        if (_page != null && !_ownsPlaywrightStack)
            throw new InvalidOperationException("Browser is attached to an external page; do not call StartBrowserAsync.");

        using var timer = _metrics.StartTimer(
            _metricNames[nameof(Wm.Metrics.StartBrowserDuration)],
            PlaywrightMetricTags.ForOperation(this, "start_browser"));
        ct.ThrowIfCancellationRequested();
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        var browserType = GetBrowserType(_playwright);
        var launchOptions = new BrowserTypeLaunchOptions {
            Headless = _options.Headless,
            Channel = _options.Channel,
            Args = _options.LaunchArguments,
            SlowMo = _options.SlowMoMilliseconds
        };

        _browser = await browserType.LaunchAsync(launchOptions).ConfigureAwait(false);
        var contextOptions = new BrowserNewContextOptions {
            ViewportSize = new ViewportSize { Width = _options.ViewportWidth, Height = _options.ViewportHeight },
            IgnoreHTTPSErrors = _options.IgnoreHttpsErrors,
            AcceptDownloads = true
        };

        if (_options.UserAgents.Count > 0)
            contextOptions.UserAgent = _options.UserAgents[0];

        _context = await _browser.NewContextAsync(contextOptions).ConfigureAwait(false);
        _context.SetDefaultTimeout(_options.NavigationTimeoutMs);
        _context.SetDefaultNavigationTimeout(_options.NavigationTimeoutMs);
        _page = await _context.NewPageAsync().ConfigureAwait(false);
        ApplyDefaultTimeouts(_page);
        _ownsPlaywrightStack = true;
        _logger.LogInformation("Playwright browser started ({BrowserKind})", _options.BrowserKind);
    }

    /// <summary>Closes context and browser when this instance owns them.</summary>
    public async Task StopBrowserAsync(CancellationToken ct = default)
    {
        if (!_ownsPlaywrightStack) {
            _logger.LogDebug("Browser not owned by this instance");
            return;
        }

        using var timer = _metrics.StartTimer(
            _metricNames[nameof(Wm.Metrics.StopBrowserDuration)],
            PlaywrightMetricTags.ForOperation(this, "stop_browser"));
        ct.ThrowIfCancellationRequested();
        try {
            _tabs?.ClearDisplayNames();
            _tabs = null;
            _frames = null;
            _dialogs = null;
            _keyboard = null;
            _cookieJar = null;
            _headerStore = null;
            if (_context != null)
                await _context.CloseAsync().ConfigureAwait(false);

            if (_browser != null)
                await _browser.CloseAsync().ConfigureAwait(false);

            _playwright?.Dispose();
            _logger.LogInformation("Playwright browser stopped");
        }
        finally {
            _context = null;
            _browser = null;
            _page = null;
            _playwright = null;
            _ownsPlaywrightStack = false;
        }
    }

    /// <summary>Sets the active page used by this façade (and automation plans).</summary>
    internal void SetActivePage(IPage page)
    {
        ArgumentHelpers.ThrowIfNull(page, nameof(page));
        _page = page;
        ApplyDefaultTimeouts(_page);
    }

    internal IPage GetRequiredPage()
    {
        EnsureStarted();
        return _page!;
    }

    internal IBrowserContext GetRequiredContext()
    {
        EnsureStarted();
        return _context ?? _page!.Context;
    }

    /// <summary>Current document URL.</summary>
    public string GetCurrentUrl()
    {
        EnsureStarted();
        return _page!.Url;
    }

    /// <summary>Current document title.</summary>
    public async Task<string> GetTitleAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return await _page!.TitleAsync().ConfigureAwait(false);
    }

    /// <summary>Full HTML source of the current page.</summary>
    public async Task<string> GetPageSourceAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return await _page!.ContentAsync().ConfigureAwait(false);
    }

    /// <summary>Scrolls the window by pixel offsets.</summary>
    public Task ScrollByAsync(int xOffset, int yOffset, CancellationToken ct = default)
    {
        EnsureStarted();
        return _page!.EvaluateAsync($"window.scrollBy({xOffset}, {yOffset});");
    }

    /// <summary>Scrolls the window to document coordinates.</summary>
    public Task ScrollToAsync(int x, int y, CancellationToken ct = default)
    {
        EnsureStarted();
        return _page!.EvaluateAsync($"window.scrollTo({x}, {y});");
    }

    /// <summary>Scrolls to the bottom of the page.</summary>
    public Task ScrollToBottomAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return _page!.EvaluateAsync(
            "window.scrollTo(0, Math.max(document.body.scrollHeight, document.documentElement.scrollHeight));");
    }

    /// <summary>Scrolls to the top of the page.</summary>
    public Task ScrollToTopAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return _page!.EvaluateAsync("window.scrollTo(0, 0);");
    }

    /// <summary>Waits until <c>document.readyState === 'complete'</c> or the navigation timeout elapses.</summary>
    public async Task WaitForDocumentReadyAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(_options.NavigationTimeoutMs);
        while (DateTime.UtcNow < deadline) {
            ct.ThrowIfCancellationRequested();
            var state = await _page!.EvaluateAsync<string>("() => document.readyState").ConfigureAwait(false);
            if (string.Equals(state, "complete", StringComparison.Ordinal))
                return;

            await Task.Delay(25, ct).ConfigureAwait(false);
        }

        throw new PlaywrightException("document.readyState did not reach 'complete' within the configured navigation timeout.");
    }

    /// <summary>Navigates to a URL (logs with optional redaction).</summary>
    public async Task NavigateToAsync(string url, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(url));
        EnsureStarted();
        await _page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        var forLog = BrowserUrlRedaction.ForLog(url, _options.MaskSensitiveUrlsInLogs);
        _logger.LogDebug("Navigated to {Url}", forLog);
    }

    internal string? TryGetCurrentUrl()
    {
        try {
            return _page?.Url;
        }
        catch {
            return null;
        }
    }

    Task IWebAutomationBrowser.NavigateAsync(string url, CancellationToken ct) => NavigateToAsync(url, ct);

    async Task IWebAutomationBrowser.ReloadAsync(CancellationToken ct)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        await _page!.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        _logger.LogDebug("Page reloaded");
    }

    Task<string> IWebAutomationBrowser.GetCurrentUrlAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentUrl());
    }

    Task<string> IWebAutomationBrowser.GetTitleAsync(CancellationToken ct) => GetTitleAsync(ct);

    Task<IWebAutomationElement> IWebAutomationBrowser.PollForElementAsync(ElementLocatorChain chain, CancellationToken ct)
        => PollForChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>> IWebAutomationBrowser.PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct)
        => PollForElementsAsync(chain, ct);

    Task<IWebAutomationElement?> IWebAutomationBrowser.GetElementAsync(ElementLocatorChain chain, CancellationToken ct)
        => GetElementChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>?> IWebAutomationBrowser.GetElementsAsync(ElementLocatorChain chain, CancellationToken ct)
        => GetElementsChainAsync(chain, ct);

    /// <summary>Captures the visible viewport as PNG.</summary>
    public async Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        return await _page!.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
    }

    Task<byte[]> IWebAutomationBrowser.TakeViewportSnapshotPngAsync(CancellationToken ct) => TakeViewportSnapshotPngAsync(ct);

    /// <summary>Polls for a descendant of <paramref name="parent" />; each attempt runs <see cref="GetDescendantElementCoreAsync" />.</summary>
    internal async Task<IWebAutomationElement> PollForDescendantElementAsync(ILocator parent, ElementLocator child, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(parent, nameof(parent));
        var locatorTag = $"{child.Kind}:{child.Value}";
        var pollTags = PlaywrightMetricTags.ForOperation(this, "poll_nested", new[] { ("locator", locatorTag) });
        using var timer = _metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, _options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await GetDescendantElementCoreAsync(parent, child, ct).ConfigureAwait(false);
                if (el != null) {
                    _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    return el;
                }

                if (attempt < max)
                    await Task.Delay(_options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Nested element not found after {max} attempts: {locatorTag}");
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            _metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            throw;
        }
    }

    /// <summary>One locator wait; no outer polling retries. Returns <see langword="null" /> if not found.</summary>
    private async Task<PlaywrightWebAutomationElement?> GetDescendantElementCoreAsync(ILocator parent, ElementLocator child, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            var loc = PlaywrightLocatorFactory.Locate(parent, child);
            await loc.WaitForAsync(
                    new LocatorWaitForOptions {
                        State = WaitForSelectorState.Visible,
                        Timeout = _options.LocatorDefaultTimeoutMs
                    })
                .ConfigureAwait(false);
            return new PlaywrightWebAutomationElement(loc, this);
        }
        catch (Exception ex) {
            if (ex is OperationCanceledException)
                throw;

            return null;
        }
    }

    /// <summary>Resolves a chained path once (first segment respects the current frame stack via <see cref="PlaywrightFrameNavigator" />). Returns <see langword="null" /> if not found.</summary>
    public async Task<IWebAutomationElement?> GetElementChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var getTags = PlaywrightMetricTags.ForOperation(this, "get_chain", new[] { ("locator", locatorDesc) });
        using var timer = _metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], getTags);
        try {
            var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (el == null) {
                _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
                _logger.LogDebug("Chained element not found: {Locator}", locatorDesc);
                return null;
            }

            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: getTags);
            _logger.LogDebug("Resolved chained element: {Locator}", locatorDesc);
            return el;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
            _metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, getTags);
            _logger.LogWarning(ex, "Failed to resolve chained element: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Polls for a chained path (first segment respects the current frame stack via <see cref="PlaywrightFrameNavigator" />); each attempt runs <see cref="GetElementChainCoreAsync" />.</summary>
    public async Task<IWebAutomationElement> PollForChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var pollTags = PlaywrightMetricTags.ForOperation(this, "poll_chain", new[] { ("locator", locatorDesc) });
        using var timer = _metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, _options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (el != null) {
                    _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    _logger.LogDebug("Found chained element: {Locator}", locatorDesc);
                    return el;
                }

                if (attempt < max)
                    await Task.Delay(_options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Chained element not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            _metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            _logger.LogWarning(ex, "Failed to find chained element after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Resolves the chain once and returns every match for the final segment. Returns <see langword="null" /> if not found.</summary>
    public async Task<IReadOnlyList<IWebAutomationElement>?> GetElementsChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var getTags = PlaywrightMetricTags.ForOperation(this, "get_many", new[] { ("locator", locatorDesc) });
        using var timer = _metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], getTags);
        try {
            var list = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (list == null) {
                _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
                _logger.LogDebug("Elements not resolved: {Locator}", locatorDesc);
                return null;
            }

            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: getTags);
            _logger.LogDebug("Resolved {Count} elements: {Locator}", list.Count, locatorDesc);
            return list;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
            _metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, getTags);
            _logger.LogWarning(ex, "Failed to resolve elements: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Waits until at least one match is visible, then returns every matching element for the final segment; each attempt runs <see cref="GetElementsChainCoreAsync" />.</summary>
    public async Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var pollTags = PlaywrightMetricTags.ForOperation(this, "poll_many", new[] { ("locator", locatorDesc) });
        using var timer = _metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, _options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var list = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (list != null) {
                    _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    _logger.LogDebug("Found {Count} elements: {Locator}", list.Count, locatorDesc);
                    return list;
                }

                if (attempt < max)
                    await Task.Delay(_options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Elements not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            _metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            _logger.LogWarning(ex, "Failed to find elements after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    private async Task<PlaywrightWebAutomationElement?> GetElementChainCoreAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            var loc = BuildChainedLocator(segments);
            await loc.WaitForAsync(
                    new LocatorWaitForOptions {
                        State = WaitForSelectorState.Visible,
                        Timeout = _options.LocatorDefaultTimeoutMs
                    })
                .ConfigureAwait(false);
            return new PlaywrightWebAutomationElement(loc, this);
        }
        catch (Exception ex) {
            if (ex is OperationCanceledException)
                throw;

            return null;
        }
    }

    private async Task<IReadOnlyList<IWebAutomationElement>?> GetElementsChainCoreAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            var loc = BuildChainedLocator(segments);
            await loc.First.WaitForAsync(
                    new LocatorWaitForOptions {
                        State = WaitForSelectorState.Visible,
                        Timeout = _options.LocatorDefaultTimeoutMs
                    })
                .ConfigureAwait(false);
            var count = await loc.CountAsync().ConfigureAwait(false);
            var list = new List<IWebAutomationElement>(count);
            for (var i = 0; i < count; i++)
                list.Add(new PlaywrightWebAutomationElement(loc.Nth(i), this));

            return list;
        }
        catch (Exception ex) {
            if (ex is OperationCanceledException)
                throw;

            return null;
        }
    }

    private ILocator BuildChainedLocator(IReadOnlyList<ElementLocator> segments)
    {
        var loc = Frames.ResolveLocator(segments[0]);
        for (var i = 1; i < segments.Count; i++)
            loc = PlaywrightLocatorFactory.Locate(loc, segments[i]);

        return loc;
    }

    private void ApplyDefaultTimeouts(IPage page)
    {
        page.SetDefaultTimeout(_options.LocatorDefaultTimeoutMs);
        page.SetDefaultNavigationTimeout(_options.NavigationTimeoutMs);
    }

    private void EnsureStarted()
        => OperationHelpers.ThrowIfNull(_page, "Browser not started. Call StartBrowserAsync first, or attach via the IPage constructor.");

    private IBrowserType GetBrowserType(IPlaywright playwright)
    {
        return _options.BrowserKind switch {
            PlaywrightBrowserKind.Chromium => playwright.Chromium,
            PlaywrightBrowserKind.Firefox => playwright.Firefox,
            PlaywrightBrowserKind.Webkit => playwright.Webkit,
            _ => throw new ArgumentOutOfRangeException(nameof(_options.BrowserKind), _options.BrowserKind, null)
        };
    }

    async Task IWebAutomationBrowser.NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct)
    {
        EnsureStarted();
        var done = false;

        void Handle(object? _, IRequest req)
        {
            if (!Volatile.Read(ref done) && onRequest(req.Url))
                Volatile.Write(ref done, true);
        }

        _page!.Request += Handle;
        try {
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
            while (!Volatile.Read(ref done))
                await Task.Delay(50, ct).ConfigureAwait(false);
        }
        finally {
            _page.Request -= Handle;
        }
    }

    private static Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Wm.Metrics.StartBrowserDuration), Constants.Metrics.StartBrowserDuration },
            { nameof(Wm.Metrics.StopBrowserDuration), Constants.Metrics.StopBrowserDuration },
            { nameof(Wm.Metrics.PollSuccess), Constants.Metrics.PollSuccess },
            { nameof(Wm.Metrics.PollFailure), Constants.Metrics.PollFailure },
            { nameof(Wm.Metrics.PollDuration), Constants.Metrics.PollDuration },
            { nameof(Wm.Metrics.TabOperationDuration), Constants.Metrics.TabOperationDuration },
            { nameof(Wm.Metrics.TabOperation), Constants.Metrics.TabOperation },
            { nameof(Wm.Metrics.FrameOperationDuration), Constants.Metrics.FrameOperationDuration },
            { nameof(Wm.Metrics.FrameOperation), Constants.Metrics.FrameOperation },
            { nameof(Wm.Metrics.AlertOperationDuration), Constants.Metrics.AlertOperationDuration },
            { nameof(Wm.Metrics.AlertOperation), Constants.Metrics.AlertOperation },
            { nameof(Wm.Metrics.KeyboardOperationDuration), Constants.Metrics.KeyboardOperationDuration },
            { nameof(Wm.Metrics.ControlInteraction), Constants.Metrics.ControlInteraction }
        };
}
