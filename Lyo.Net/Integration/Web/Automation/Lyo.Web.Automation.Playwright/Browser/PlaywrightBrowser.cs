using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Playwright session façade: tabs, frames, dialogs, keyboard, metrics, and <see cref="IWebAutomationBrowser" />.</summary>
public sealed class PlaywrightBrowser : IWebAutomationBrowser, IDisposable, IAsyncDisposable
{
    private readonly Dictionary<string, string> _metricNames;
    private IBrowser? _browser;
    private PlaywrightCookieJar? _cookieJar;
    private PlaywrightDialogs? _dialogs;
    private bool _disposed;
    private PlaywrightFrameNavigator? _frames;
    private PlaywrightHeaderStore? _headerStore;
    private PlaywrightKeyboard? _keyboard;
    private bool _ownsPlaywrightStack;

    private IPlaywright? _playwright;

    private PlaywrightTabManager? _tabs;

    /// <summary>Correlation id when created from <see cref="IPlaywrightBrowserService.CreateSession" />; otherwise <see cref="Guid.Empty" />.</summary>
    public Guid SessionId => ExecutionContext?.SessionId ?? Guid.Empty;

    /// <summary>Session-scoped paths when created from <see cref="IPlaywrightBrowserService.CreateSession" />.</summary>
    public PlaywrightExecutionContext? ExecutionContext { get; }

    /// <summary>Configuration used for this session.</summary>
    internal PlaywrightBrowserOptions Options { get; }

    internal ILogger<PlaywrightBrowser> Logger { get; }

    internal IMetrics Metrics { get; }

    internal string SessionIdLabel => SessionId == Guid.Empty ? "none" : SessionId.ToString("N");

    /// <summary>Underlying Playwright page for the active tab.</summary>
    public IPage? Page { get; private set; }

    /// <summary>Underlying browser context when started.</summary>
    public IBrowserContext? Context { get; private set; }

    /// <summary>Tab / page management.</summary>
    public PlaywrightTabManager Tabs => _tabs ??= new(this, Logger);

    /// <summary>Nested iframe navigation.</summary>
    public PlaywrightFrameNavigator Frames => _frames ??= new(this);

    /// <summary>Dialog helpers.</summary>
    public PlaywrightDialogs Dialogs => _dialogs ??= new(this);

    /// <summary>Keyboard input.</summary>
    public PlaywrightKeyboard Keyboard => _keyboard ??= new(this);

    /// <summary>Creates a browser that will be launched via <see cref="StartBrowserAsync" />.</summary>
    public PlaywrightBrowser(
        PlaywrightBrowserOptions options,
        PlaywrightExecutionContext? executionContext = null,
        ILogger<PlaywrightBrowser>? logger = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        Options = options;
        ExecutionContext = executionContext ?? PlaywrightExecutionContextFactory.Create(options, Guid.NewGuid());
        var baseLogger = logger ?? NullLogger<PlaywrightBrowser>.Instance;
        Logger = ExecutionContext.BuildLogger(baseLogger);
        Metrics = Options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Attaches to an existing page (does not own Playwright resources unless <see cref="PlaywrightBrowserOptions.CloseOwnedResourcesOnDispose" /> is set).</summary>
    public PlaywrightBrowser(IPage page, PlaywrightBrowserOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(page, nameof(page));
        Options = options ?? new PlaywrightBrowserOptions();
        Logger = NullLogger<PlaywrightBrowser>.Instance;
        Metrics = NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
        Page = page;
        Context = page.Context;
        _ownsPlaywrightStack = false;
        ApplyDefaultTimeouts(Page);
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
        if (_ownsPlaywrightStack && Options.CloseOwnedResourcesOnDispose) {
            try {
                if (Context != null)
                    await Context.CloseAsync().ConfigureAwait(false);
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

        Context = null;
        _browser = null;
        Page = null;
        _playwright = null;
        await (ExecutionContext?.DisposeAsync() ?? default).ConfigureAwait(false);
        GC.SuppressFinalize(this);
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
        if (_ownsPlaywrightStack && Options.CloseOwnedResourcesOnDispose) {
            try {
                Context?.CloseAsync().GetAwaiter().GetResult();
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

        Context = null;
        _browser = null;
        Page = null;
        _playwright = null;
        ExecutionContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public IBrowserCookies? CookieJar => Context != null ? _cookieJar ??= new(this) : null;

    /// <inheritdoc />
    public IBrowserHeaders? ExtraHeaders => Context != null ? _headerStore ??= new(this) : null;

    /// <summary>Full HTML source of the current page.</summary>
    public async Task<string> GetPageSourceAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return await Page!.ContentAsync().ConfigureAwait(false);
    }

    Task IWebAutomationBrowser.NavigateAsync(string url, CancellationToken ct) => NavigateToAsync(url, ct);

    async Task IWebAutomationBrowser.ReloadAsync(CancellationToken ct)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        await Page!.ReloadAsync(new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        Logger.LogDebug("Page reloaded");
    }

    Task<string> IWebAutomationBrowser.GetCurrentUrlAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentUrl());
    }

    Task<string> IWebAutomationBrowser.GetTitleAsync(CancellationToken ct) => GetTitleAsync(ct);

    Task<IWebAutomationElement> IWebAutomationBrowser.PollForElementAsync(ElementLocatorChain chain, CancellationToken ct) => PollForChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>> IWebAutomationBrowser.PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct) => PollForElementsAsync(chain, ct);

    Task<IWebAutomationElement?> IWebAutomationBrowser.GetElementAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>?> IWebAutomationBrowser.GetElementsAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementsChainAsync(chain, ct);

    Task<byte[]> IWebAutomationBrowser.TakeViewportSnapshotPngAsync(CancellationToken ct) => TakeViewportSnapshotPngAsync(ct);

    async Task IWebAutomationBrowser.NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct)
    {
        EnsureStarted();
        var done = false;

        void Handle(object? _, IRequest req)
        {
            if (!Volatile.Read(ref done) && onRequest(req.Url))
                Volatile.Write(ref done, true);
        }

        Page!.Request += Handle;
        try {
            await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
            while (!Volatile.Read(ref done))
                await Task.Delay(50, ct).ConfigureAwait(false);
        }
        finally {
            Page.Request -= Handle;
        }
    }

    /// <summary>Resolved metric name for members of <see cref="Constants.Metrics" /> (Playwright-specific series).</summary>
    internal string ResolveMetric(string metricMemberName) => _metricNames[metricMemberName];

    /// <summary>Begins a structured logging scope (<c>session_id</c>, <c>operation</c>, <c>url</c>).</summary>
    public IDisposable? BeginOperationScope(string operation, string? urlOverride = null)
    {
        var url = urlOverride ?? TryGetCurrentUrl();
        return Logger.BeginScope(
            new Dictionary<string, object?> {
                ["session_id"] = SessionIdLabel, ["operation"] = operation, ["url"] = url == null ? null : BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs)
            });
    }

    /// <summary>Launches the browser, context, and initial page.</summary>
    public async Task StartBrowserAsync(CancellationToken ct = default)
    {
        if (Page != null && _ownsPlaywrightStack) {
            Logger.LogDebug("Browser already started");
            return;
        }

        OperationHelpers.ThrowIf(Page != null && !_ownsPlaywrightStack, "Browser is attached to an external page; do not call StartBrowserAsync.");

        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StartBrowserDuration)], PlaywrightMetricTags.ForOperation(this, "start_browser"));
        ct.ThrowIfCancellationRequested();
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        var browserType = GetBrowserType(_playwright);
        var launchOptions = new BrowserTypeLaunchOptions {
            Headless = Options.Headless,
            Channel = Options.Channel,
            Args = Options.LaunchArguments,
            SlowMo = Options.SlowMoMilliseconds
        };

        _browser = await browserType.LaunchAsync(launchOptions).ConfigureAwait(false);
        var contextOptions = new BrowserNewContextOptions {
            ViewportSize = new() { Width = Options.ViewportWidth, Height = Options.ViewportHeight }, IgnoreHTTPSErrors = Options.IgnoreHttpsErrors, AcceptDownloads = true
        };

        if (Options.UserAgents.Count > 0)
            contextOptions.UserAgent = Options.UserAgents[0];

        Context = await _browser.NewContextAsync(contextOptions).ConfigureAwait(false);
        Context.SetDefaultTimeout(Options.NavigationTimeoutMs);
        Context.SetDefaultNavigationTimeout(Options.NavigationTimeoutMs);
        Page = await Context.NewPageAsync().ConfigureAwait(false);
        ApplyDefaultTimeouts(Page);
        _ownsPlaywrightStack = true;
        Logger.LogInformation("Playwright browser started ({BrowserKind})", Options.BrowserKind);
    }

    /// <summary>Closes context and browser when this instance owns them.</summary>
    public async Task StopBrowserAsync(CancellationToken ct = default)
    {
        if (!_ownsPlaywrightStack) {
            Logger.LogDebug("Browser not owned by this instance");
            return;
        }

        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StopBrowserDuration)], PlaywrightMetricTags.ForOperation(this, "stop_browser"));
        ct.ThrowIfCancellationRequested();
        try {
            _tabs?.ClearDisplayNames();
            _tabs = null;
            _frames = null;
            _dialogs = null;
            _keyboard = null;
            _cookieJar = null;
            _headerStore = null;
            if (Context != null)
                await Context.CloseAsync().ConfigureAwait(false);

            if (_browser != null)
                await _browser.CloseAsync().ConfigureAwait(false);

            _playwright?.Dispose();
            Logger.LogInformation("Playwright browser stopped");
        }
        finally {
            Context = null;
            _browser = null;
            Page = null;
            _playwright = null;
            _ownsPlaywrightStack = false;
        }
    }

    /// <summary>Sets the active page used by this façade (and automation plans).</summary>
    internal void SetActivePage(IPage page)
    {
        ArgumentHelpers.ThrowIfNull(page, nameof(page));
        Page = page;
        ApplyDefaultTimeouts(Page);
    }

    internal IPage GetRequiredPage()
    {
        EnsureStarted();
        return Page!;
    }

    internal IBrowserContext GetRequiredContext()
    {
        EnsureStarted();
        return Context ?? Page!.Context;
    }

    /// <summary>Current document URL.</summary>
    public string GetCurrentUrl()
    {
        EnsureStarted();
        return Page!.Url;
    }

    /// <summary>Current document title.</summary>
    public async Task<string> GetTitleAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return await Page!.TitleAsync().ConfigureAwait(false);
    }

    /// <summary>Scrolls the window by pixel offsets.</summary>
    public Task ScrollByAsync(int xOffset, int yOffset, CancellationToken ct = default)
    {
        EnsureStarted();
        return Page!.EvaluateAsync($"window.scrollBy({xOffset}, {yOffset});");
    }

    /// <summary>Scrolls the window to document coordinates.</summary>
    public Task ScrollToAsync(int x, int y, CancellationToken ct = default)
    {
        EnsureStarted();
        return Page!.EvaluateAsync($"window.scrollTo({x}, {y});");
    }

    /// <summary>Scrolls to the bottom of the page.</summary>
    public Task ScrollToBottomAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return Page!.EvaluateAsync("window.scrollTo(0, Math.max(document.body.scrollHeight, document.documentElement.scrollHeight));");
    }

    /// <summary>Scrolls to the top of the page.</summary>
    public Task ScrollToTopAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        return Page!.EvaluateAsync("window.scrollTo(0, 0);");
    }

    /// <summary>Waits until <c>document.readyState === 'complete'</c> or the navigation timeout elapses.</summary>
    public async Task WaitForDocumentReadyAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(Options.NavigationTimeoutMs);
        while (DateTime.UtcNow < deadline) {
            ct.ThrowIfCancellationRequested();
            var state = await Page!.EvaluateAsync<string>("() => document.readyState").ConfigureAwait(false);
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
        await Page!.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        var forLog = BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs);
        Logger.LogDebug("Navigated to {Url}", forLog);
    }

    internal string? TryGetCurrentUrl()
    {
        try {
            return Page?.Url;
        }
        catch {
            return null;
        }
    }

    /// <summary>Captures the visible viewport as PNG.</summary>
    public async Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        return await Page!.ScreenshotAsync(new() { Type = ScreenshotType.Png }).ConfigureAwait(false);
    }

    /// <summary>Polls for a descendant of <paramref name="parent" />; each attempt runs <see cref="GetDescendantElementCoreAsync" />.</summary>
    internal async Task<IWebAutomationElement> PollForDescendantElementAsync(ILocator parent, ElementLocator child, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(parent, nameof(parent));
        var locatorTag = $"{child.Kind}:{child.Value}";
        var pollTags = PlaywrightMetricTags.ForOperation(this, "poll_nested", new[] { ("locator", locatorTag) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await GetDescendantElementCoreAsync(parent, child, ct).ConfigureAwait(false);
                if (el != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: pollTags);
                    return el;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Nested element not found after {max} attempts: {locatorTag}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, pollTags);
            throw;
        }
    }

    /// <summary>One locator wait; no outer polling retries. Returns <see langword="null" /> if not found.</summary>
    private async Task<PlaywrightWebAutomationElement?> GetDescendantElementCoreAsync(ILocator parent, ElementLocator child, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            var loc = PlaywrightLocatorFactory.Locate(parent, child);
            await loc.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = Options.LocatorDefaultTimeoutMs }).ConfigureAwait(false);
            return new(loc, this);
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
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], getTags);
        try {
            var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (el == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: getTags);
                Logger.LogDebug("Chained element not found: {Locator}", locatorDesc);
                return null;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: getTags);
            Logger.LogDebug("Resolved chained element: {Locator}", locatorDesc);
            return el;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: getTags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, getTags);
            Logger.LogWarning(ex, "Failed to resolve chained element: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>
    /// Polls for a chained path (first segment respects the current frame stack via <see cref="PlaywrightFrameNavigator" />); each attempt runs
    /// <see cref="GetElementChainCoreAsync" />.
    /// </summary>
    public async Task<IWebAutomationElement> PollForChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var pollTags = PlaywrightMetricTags.ForOperation(this, "poll_chain", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (el != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: pollTags);
                    Logger.LogDebug("Found chained element: {Locator}", locatorDesc);
                    return el;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Chained element not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, pollTags);
            Logger.LogWarning(ex, "Failed to find chained element after retries: {Locator}", locatorDesc);
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
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], getTags);
        try {
            var list = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (list == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: getTags);
                Logger.LogDebug("Elements not resolved: {Locator}", locatorDesc);
                return null;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: getTags);
            Logger.LogDebug("Resolved {Count} elements: {Locator}", list.Count, locatorDesc);
            return list;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: getTags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, getTags);
            Logger.LogWarning(ex, "Failed to resolve elements: {Locator}", locatorDesc);
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
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var list = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (list != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: pollTags);
                    Logger.LogDebug("Found {Count} elements: {Locator}", list.Count, locatorDesc);
                    return list;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Elements not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, pollTags);
            Logger.LogWarning(ex, "Failed to find elements after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    private async Task<PlaywrightWebAutomationElement?> GetElementChainCoreAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            var loc = BuildChainedLocator(segments);
            await loc.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = Options.LocatorDefaultTimeoutMs }).ConfigureAwait(false);
            return new(loc, this);
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
            await loc.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = Options.LocatorDefaultTimeoutMs }).ConfigureAwait(false);
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
        page.SetDefaultTimeout(Options.LocatorDefaultTimeoutMs);
        page.SetDefaultNavigationTimeout(Options.NavigationTimeoutMs);
    }

    private void EnsureStarted() => OperationHelpers.ThrowIfNull(Page, "Browser not started. Call StartBrowserAsync first, or attach via the IPage constructor.");

    private IBrowserType GetBrowserType(IPlaywright playwright)
        => Options.BrowserKind switch {
            PlaywrightBrowserKind.Chromium => playwright.Chromium,
            PlaywrightBrowserKind.Firefox => playwright.Firefox,
            PlaywrightBrowserKind.Webkit => playwright.Webkit,
            var _ => throw new ArgumentOutOfRangeException(nameof(Options.BrowserKind), Options.BrowserKind, null)
        };

    private static Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.StartBrowserDuration), Constants.Metrics.StartBrowserDuration },
            { nameof(Constants.Metrics.StopBrowserDuration), Constants.Metrics.StopBrowserDuration },
            { nameof(Constants.Metrics.PollSuccess), Constants.Metrics.PollSuccess },
            { nameof(Constants.Metrics.PollFailure), Constants.Metrics.PollFailure },
            { nameof(Constants.Metrics.PollDuration), Constants.Metrics.PollDuration },
            { nameof(Constants.Metrics.TabOperationDuration), Constants.Metrics.TabOperationDuration },
            { nameof(Constants.Metrics.TabOperation), Constants.Metrics.TabOperation },
            { nameof(Constants.Metrics.FrameOperationDuration), Constants.Metrics.FrameOperationDuration },
            { nameof(Constants.Metrics.FrameOperation), Constants.Metrics.FrameOperation },
            { nameof(Constants.Metrics.AlertOperationDuration), Constants.Metrics.AlertOperationDuration },
            { nameof(Constants.Metrics.AlertOperation), Constants.Metrics.AlertOperation },
            { nameof(Constants.Metrics.KeyboardOperationDuration), Constants.Metrics.KeyboardOperationDuration },
            { nameof(Constants.Metrics.ControlInteraction), Constants.Metrics.ControlInteraction }
        };
}