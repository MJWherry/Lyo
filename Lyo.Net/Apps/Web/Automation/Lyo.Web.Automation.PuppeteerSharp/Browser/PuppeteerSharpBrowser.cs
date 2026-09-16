using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.PuppeteerSharp.Configuration;
using Lyo.Web.Automation.PuppeteerSharp.Core;
using Lyo.Web.Automation.PuppeteerSharp.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PuppeteerSharp;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary>PuppeteerSharp session façade: tabs, frames, dialogs, keyboard, metrics, and <see cref="IWebAutomationBrowser" />.</summary>
public sealed class PuppeteerSharpBrowser : IWebAutomationBrowser, IDisposable, IAsyncDisposable
{
    private readonly Dictionary<string, string> _extraHeaders = [];
    private readonly Dictionary<string, string> _metricNames;
    private PuppeteerSharpBrowserTabs? _automationTabs;
    private IBrowser? _browser;
    private PuppeteerSharpCookieJar? _cookieJar;
    private PuppeteerSharpDialogs? _dialogs;
    private bool _disposed;
    private PuppeteerSharpFrameNavigator? _frames;
    private PuppeteerSharpHeaderStore? _headerStore;
    private PuppeteerSharpKeyboard? _keyboard;
    private PuppeteerSharpTabManager? _nativeTabs;
    private bool _ownsBrowser;

    /// <summary>Creates a browser that <see cref="StartBrowserAsync" /> will launch.</summary>
    public PuppeteerSharpBrowser(
        PuppeteerSharpBrowserOptions options,
        PuppeteerSharpExecutionContext? executionContext = null,
        ILogger<PuppeteerSharpBrowser>? logger = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        Options = options;
        ExecutionContext = executionContext ?? PuppeteerSharpExecutionContextFactory.Create(options, LyoGuid.CreateV7());
        var baseLogger = logger ?? NullLogger<PuppeteerSharpBrowser>.Instance;
        Logger = ExecutionContext.BuildBrowserLogger(baseLogger);
        Metrics = Options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Attaches to an existing page. This instance does not own the browser unless <see cref="PuppeteerSharpBrowserOptions.CloseOwnedResourcesOnDispose" /> is set.</summary>
    public PuppeteerSharpBrowser(IPage page, PuppeteerSharpBrowserOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(page);
        Options = options ?? new PuppeteerSharpBrowserOptions();
        Logger = NullLogger<PuppeteerSharpBrowser>.Instance;
        Metrics = NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
        Page = page;
        Context = page.BrowserContext;
        _ownsBrowser = false;
        ApplyDefaultTimeouts(Page);
    }

    /// <summary>Correlation id when created from <see cref="IPuppeteerSharpBrowserService.CreateSession(PuppeteerSharpSessionOptions?)" />; otherwise <see cref="Guid.Empty" />.</summary>
    public Guid SessionId => ExecutionContext?.SessionId ?? Guid.Empty;

    /// <summary>Session-scoped paths when created from the browser service.</summary>
    public PuppeteerSharpExecutionContext? ExecutionContext { get; }

    /// <summary>Configuration used for this session.</summary>
    internal PuppeteerSharpBrowserOptions Options { get; }

    internal ILogger<PuppeteerSharpBrowser> Logger { get; }

    internal IMetrics Metrics { get; }

    internal string SessionIdLabel => SessionId == Guid.Empty ? "none" : SessionId.ToString("N");

    internal int LocatorTimeoutMs
    {
        get {
            var ms = (int)Options.LocatorDefaultTimeoutMs;
            return ms < 1 ? 1 : ms;
        }
    }

    /// <summary>Active Puppeteer page.</summary>
    public IPage? Page { get; private set; }

    /// <summary>Browser context when started.</summary>
    public IBrowserContext? Context { get; private set; }

    /// <summary>Engine-native tab management. Prefer <see cref="Tabs" /> for cross-engine plans.</summary>
    public PuppeteerSharpTabManager NativeTabs => _nativeTabs ??= new(this, Logger);

    /// <summary>Nested iframe navigation.</summary>
    public PuppeteerSharpFrameNavigator Frames => _frames ??= new(this);

    /// <summary>Dialog helpers.</summary>
    public PuppeteerSharpDialogs Dialogs => _dialogs ??= new(this);

    /// <summary>Keyboard input.</summary>
    public PuppeteerSharpKeyboard Keyboard => _keyboard ??= new(this);

    /// <inheritdoc />
    public IWebAutomationTabs Tabs => _automationTabs ??= new(this);

    /// <inheritdoc />
    public IBrowserCookies? CookieJar => Context != null ? _cookieJar ??= new(this) : null;

    /// <inheritdoc />
    public IBrowserHeaders? ExtraHeaders => Context != null ? _headerStore ??= new(this) : null;

    /// <inheritdoc />
    public IWebAutomationNavigator Navigator => this;

    /// <inheritdoc />
    public IWebAutomationPage CurrentPage => this;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        ClearHelpers();
        if (_ownsBrowser && Options.CloseOwnedResourcesOnDispose)
            await CloseOwnedAsync().ConfigureAwait(false);

        Page = null;
        Context = null;
        _browser = null;
        await (ExecutionContext?.DisposeAsync() ?? default).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ClearHelpers();
        if (_ownsBrowser && Options.CloseOwnedResourcesOnDispose) {
            try {
                CloseOwnedAsync().GetAwaiter().GetResult();
            }
            catch {
                // best-effort
            }
        }

        Page = null;
        Context = null;
        _browser = null;
        ExecutionContext?.Dispose();
    }

    /// <summary>Full HTML source of the current page.</summary>
    public async Task<string> GetPageSourceAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        return await Page!.GetContentAsync().ConfigureAwait(false);
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
        ct.ThrowIfCancellationRequested();
        return await Page!.GetTitleAsync().ConfigureAwait(false);
    }

    /// <summary>Navigates to a URL.</summary>
    public async Task NavigateToAsync(string url, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        EnsureStarted();
        await Page!.GoToAsync(url, NavigationOptions(ct)).ConfigureAwait(false);
        Logger.LogDebug("Navigated to {Url}", BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs));
    }

    /// <summary>Launches Chrome, a browser context, and the initial page.</summary>
    public async Task StartBrowserAsync(CancellationToken ct = default)
    {
        if (Page != null && _ownsBrowser) {
            Logger.LogDebug("Browser already started");
            return;
        }

        OperationHelpers.ThrowIf(Page != null && !_ownsBrowser, "Browser is attached to an external page; do not call StartBrowserAsync.");
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StartBrowserDuration)], PuppeteerSharpMetricTags.ForOperation(this, "start_browser"));
        ct.ThrowIfCancellationRequested();
        Options.ApplySessionViewport();
        var executablePath = ResolveExecutablePath();
        var launch = new LaunchOptions {
            Headless = Options.Headless,
            ExecutablePath = executablePath,
            Args = Options.ResolveLaunchArguments().ToArray(),
            SlowMo = Options.SlowMoMilliseconds,
            UserDataDir = ExecutionContext?.BrowserUserDataDirectory,
            AcceptInsecureCerts = Options.IgnoreHttpsErrors,
            DefaultViewport = new ViewPortOptions { Width = Options.ViewportWidth, Height = Options.ViewportHeight },
            Timeout = Options.NavigationTimeoutMs,
            WaitForInitialPage = false
        };

        _browser = await Puppeteer.LaunchAsync(launch).ConfigureAwait(false);
        var contextOptions = new BrowserContextOptions();
        if (!string.IsNullOrWhiteSpace(Options.ProxyUrl))
            contextOptions.ProxyServer = Options.ProxyUrl;

        Context = await _browser.CreateBrowserContextAsync(contextOptions).ConfigureAwait(false);
        Page = await Context.NewPageAsync().ConfigureAwait(false);
        await ApplyPageDefaultsAsync(Page).ConfigureAwait(false);
        _ownsBrowser = true;
        Logger.LogInformation("PuppeteerSharp browser started ({BrowserKind}) at {BrowserExePath}", Options.BrowserKind, executablePath);
    }

    /// <summary>Closes the context and browser when this instance owns them.</summary>
    public async Task StopBrowserAsync(CancellationToken ct = default)
    {
        if (!_ownsBrowser) {
            Logger.LogDebug("Browser not owned by this instance");
            return;
        }

        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StopBrowserDuration)], PuppeteerSharpMetricTags.ForOperation(this, "stop_browser"));
        ct.ThrowIfCancellationRequested();
        try {
            ClearHelpers();
            await CloseOwnedAsync().ConfigureAwait(false);
            Logger.LogInformation("PuppeteerSharp browser stopped");
        }
        finally {
            Context = null;
            _browser = null;
            Page = null;
            _ownsBrowser = false;
        }
    }

    /// <summary>Captures the visible viewport as PNG.</summary>
    public async Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        return await Page!.ScreenshotDataAsync(new ScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
    }

    /// <summary>Begins a logging scope with session id, operation, and URL.</summary>
    public IDisposable? BeginOperationScope(string operation, string? urlOverride = null)
    {
        var url = urlOverride ?? TryGetCurrentUrl();
        return Logger.BeginScope(
            new Dictionary<string, object?> {
                ["session_id"] = SessionIdLabel,
                ["operation"] = operation,
                ["url"] = url == null ? null : BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs)
            });
    }

    internal string ResolveMetric(string metricMemberName) => _metricNames[metricMemberName];

    internal string? TryGetCurrentUrl()
    {
        try {
            return Page?.Url;
        }
        catch {
            return null;
        }
    }

    internal void SetActivePage(IPage page)
    {
        ArgumentHelpers.ThrowIfNull(page);
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
        return Context ?? Page!.BrowserContext;
    }

    internal NavigationOptions NavigationOptions(CancellationToken ct)
        => new() { Timeout = Options.NavigationTimeoutMs, WaitUntil = [Options.NavigationWaitUntil], CancellationToken = ct };

    internal async Task ApplyPageDefaultsAsync(IPage page)
    {
        ApplyDefaultTimeouts(page);
        await page.SetViewportAsync(new ViewPortOptions { Width = Options.ViewportWidth, Height = Options.ViewportHeight }).ConfigureAwait(false);
        var userAgent = Options.ResolveContextUserAgent();
        if (userAgent != null)
            await page.SetUserAgentAsync(userAgent).ConfigureAwait(false);

        if (_extraHeaders.Count > 0)
            await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>(_extraHeaders)).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(Options.ProxyUsername))
            await page.AuthenticateAsync(new Credentials { Username = Options.ProxyUsername, Password = Options.ProxyPassword }).ConfigureAwait(false);
    }

    internal async Task SetExtraHeadersAsync(IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _extraHeaders.Clear();
        foreach (var pair in headers)
            _extraHeaders[pair.Key] = pair.Value;

        if (Context == null)
            return;

        var copy = new Dictionary<string, string>(_extraHeaders);
        foreach (var page in await Context.PagesAsync().ConfigureAwait(false))
            await page.SetExtraHttpHeadersAsync(copy).ConfigureAwait(false);
    }

    internal async Task<IWebAutomationElement> PollForDescendantElementAsync(IElementHandle parent, ElementLocator child, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(parent);
        var locatorTag = $"{child.Kind}:{child.Value}";
        var pollTags = PuppeteerSharpMetricTags.ForOperation(this, "poll_nested", [("locator", locatorTag)]);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await TryWaitAsync(() => parent.WaitForSelectorAsync(PuppeteerSharpLocatorFactory.ToSelector(child), VisibleWait(ct)), ct).ConfigureAwait(false);
                if (el != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: pollTags);
                    return new PuppeteerSharpWebAutomationElement(el, this);
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

    Task IWebAutomationNavigator.NavigateAsync(string url, CancellationToken ct) => NavigateToAsync(url, ct);

    async Task IWebAutomationNavigator.ReloadAsync(CancellationToken ct)
    {
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        await Page!.ReloadAsync(Options.NavigationTimeoutMs, [Options.NavigationWaitUntil]).ConfigureAwait(false);
        Logger.LogDebug("Page reloaded");
    }

    async Task IWebAutomationNavigator.NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct)
    {
        EnsureStarted();
        ArgumentHelpers.ThrowIfNull(onRequest);
        var done = 0;
        void Handle(object? _, RequestEventArgs e)
        {
            if (Volatile.Read(ref done) == 0 && onRequest(e.Request.Url))
                Volatile.Write(ref done, 1);
        }

        Page!.Request += Handle;
        try {
            await Page.GoToAsync(url, NavigationOptions(ct)).ConfigureAwait(false);
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(Options.NavigationTimeoutMs);
            while (Volatile.Read(ref done) == 0) {
                ct.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("onRequest did not match any request URL within the configured navigation timeout.");

                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
        finally {
            Page.Request -= Handle;
        }
    }

    async Task<IReadOnlyList<string>> IWebAutomationNavigator.CaptureNetworkAsync(string? urlPattern, int durationMilliseconds, CancellationToken ct)
    {
        EnsureStarted();
        ArgumentHelpers.ThrowIfNegative(durationMilliseconds);
        var urls = new List<string>();
        Page!.Request += Handle;
        try {
            if (durationMilliseconds > 0)
                await Task.Delay(durationMilliseconds, ct).ConfigureAwait(false);
        }
        finally {
            Page.Request -= Handle;
        }

        lock (urls)
            return urls.ToArray();

        void Handle(object? _, RequestEventArgs e)
        {
            var url = e.Request.Url;
            if (!NetworkUrlMatcher.Matches(urlPattern, url))
                return;

            lock (urls) {
                if (!urls.Contains(url))
                    urls.Add(url);
            }
        }
    }

    Task<string> IWebAutomationPage.GetCurrentUrlAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentUrl());
    }

    Task<string> IWebAutomationPage.GetTitleAsync(CancellationToken ct) => GetTitleAsync(ct);

    Task<IWebAutomationElement> IWebAutomationPage.PollForElementAsync(ElementLocatorChain chain, CancellationToken ct) => PollForChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>> IWebAutomationPage.PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct) => PollForElementsAsync(chain, ct);

    Task<IWebAutomationElement?> IWebAutomationPage.GetElementAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>?> IWebAutomationPage.GetElementsAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementsChainAsync(chain, ct);

    Task<byte[]> IWebAutomationPage.TakeViewportSnapshotPngAsync(CancellationToken ct) => TakeViewportSnapshotPngAsync(ct);

    async Task IWebAutomationPage.SetViewportSizeAsync(int width, int height, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIf(width <= 0, "Width must be positive.", nameof(width));
        ArgumentHelpers.ThrowIf(height <= 0, "Height must be positive.", nameof(height));
        EnsureStarted();
        ct.ThrowIfCancellationRequested();
        await Page!.SetViewportAsync(new ViewPortOptions { Width = width, Height = height }).ConfigureAwait(false);
        Options.ViewportWidth = width;
        Options.ViewportHeight = height;
        if (Options.ScreenWidth < width)
            Options.ScreenWidth = width;
        if (Options.ScreenHeight < height)
            Options.ScreenHeight = height;
    }

    private async Task<IWebAutomationElement?> GetElementChainAsync(ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(chain);
        var segments = chain.Segments;
        var locatorDesc = Describe(segments);
        var tags = PuppeteerSharpMetricTags.ForOperation(this, "get_chain", [("locator", locatorDesc)]);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], tags);
        try {
            var handle = await WaitChainAsync(segments, ct).ConfigureAwait(false);
            if (handle == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
                Logger.LogDebug("Chained element not found: {Locator}", locatorDesc);
                return null;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: tags);
            return new PuppeteerSharpWebAutomationElement(handle, this);
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, tags);
            throw;
        }
    }

    private async Task<IWebAutomationElement> PollForChainAsync(ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(chain);
        var segments = chain.Segments;
        var locatorDesc = Describe(segments);
        var tags = PuppeteerSharpMetricTags.ForOperation(this, "poll_chain", [("locator", locatorDesc)]);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], tags);
        try {
            var found = await PollAsync(async () => {
                        var handle = await WaitChainAsync(segments, ct).ConfigureAwait(false);
                        return handle == null ? null : new PuppeteerSharpWebAutomationElement(handle, this);
                    }, locatorDesc, ct)
                .ConfigureAwait(false);
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: tags);
            return found;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, tags);
            throw;
        }
    }

    private async Task<IReadOnlyList<IWebAutomationElement>?> GetElementsChainAsync(ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(chain);
        var segments = chain.Segments;
        var locatorDesc = Describe(segments);
        var tags = PuppeteerSharpMetricTags.ForOperation(this, "get_many", [("locator", locatorDesc)]);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], tags);
        try {
            var list = await QueryChainAsync(segments, ct).ConfigureAwait(false);
            if (list == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
                return null;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: tags);
            return list;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, tags);
            throw;
        }
    }

    private async Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(chain);
        var segments = chain.Segments;
        var locatorDesc = Describe(segments);
        var tags = PuppeteerSharpMetricTags.ForOperation(this, "poll_many", [("locator", locatorDesc)]);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], tags);
        try {
            var found = await PollAsync(() => QueryChainAsync(segments, ct), locatorDesc, ct).ConfigureAwait(false);
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: tags);
            return found;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: tags);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, tags);
            throw;
        }
    }

    private async Task<T> PollAsync<T>(Func<Task<T?>> attempt, string locatorDesc, CancellationToken ct) where T : class
    {
        var max = Math.Max(1, Options.PollingMaxAttempts);
        for (var i = 1; i <= max; i++) {
            ct.ThrowIfCancellationRequested();
            var found = await attempt().ConfigureAwait(false);
            if (found != null)
                return found;

            if (i < max)
                await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Element not found after {max} attempts: {locatorDesc}");
    }

    private async Task<IElementHandle?> WaitChainAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        var frame = await Frames.CurrentFrameAsync(ct).ConfigureAwait(false);
        IElementHandle? handle = null;
        for (var i = 0; i < segments.Count; i++) {
            var selector = PuppeteerSharpLocatorFactory.ToSelector(segments[i]);
            handle = await TryWaitAsync(
                    () => i == 0 ? frame.WaitForSelectorAsync(selector, VisibleWait(ct)) : handle!.WaitForSelectorAsync(selector, VisibleWait(ct)), ct)
                .ConfigureAwait(false);
            if (handle == null)
                return null;
        }

        return handle;
    }

    private async Task<IReadOnlyList<IWebAutomationElement>?> QueryChainAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        var frame = await Frames.CurrentFrameAsync(ct).ConfigureAwait(false);
        IElementHandle? parent = null;
        for (var i = 0; i < segments.Count - 1; i++) {
            var selector = PuppeteerSharpLocatorFactory.ToSelector(segments[i]);
            parent = await TryWaitAsync(
                    () => i == 0 ? frame.WaitForSelectorAsync(selector, VisibleWait(ct)) : parent!.WaitForSelectorAsync(selector, VisibleWait(ct)), ct)
                .ConfigureAwait(false);
            if (parent == null)
                return null;
        }

        var last = PuppeteerSharpLocatorFactory.ToSelector(segments[segments.Count - 1]);
        var first = await TryWaitAsync(
                () => parent == null ? frame.WaitForSelectorAsync(last, VisibleWait(ct)) : parent.WaitForSelectorAsync(last, VisibleWait(ct)), ct)
            .ConfigureAwait(false);
        if (first == null)
            return null;

        var handles = parent == null ? await frame.QuerySelectorAllAsync(last).ConfigureAwait(false) : await parent.QuerySelectorAllAsync(last).ConfigureAwait(false);
        if (handles.Length == 0)
            return null;

        var list = new List<IWebAutomationElement>(handles.Length);
        foreach (var handle in handles)
            list.Add(new PuppeteerSharpWebAutomationElement(handle, this));

        return list;
    }

    private async Task<IElementHandle?> TryWaitAsync(Func<Task<IElementHandle?>> wait, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            return await wait().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            return null;
        }
    }

    private WaitForSelectorOptions VisibleWait(CancellationToken ct) => new() { Visible = true, Timeout = LocatorTimeoutMs, CancellationToken = ct };

    private static string Describe(IReadOnlyList<ElementLocator> segments) => string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));

    private void ApplyDefaultTimeouts(IPage page)
    {
        page.DefaultTimeout = LocatorTimeoutMs;
        page.DefaultNavigationTimeout = Options.NavigationTimeoutMs;
    }

    private string ResolveExecutablePath()
    {
        var path = string.IsNullOrWhiteSpace(Options.BrowserExePath) ? AutomationBrowserDetector.Detect(Options.BrowserKind) : Options.BrowserExePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new ConfigurationException($"BrowserExePath is not set to an existing {Options.BrowserKind} executable.", nameof(PuppeteerSharpBrowserOptions.BrowserExePath));

        return path;
    }

    private void EnsureStarted() => OperationHelpers.ThrowIfNull(Page, "Browser not started. Call StartBrowserAsync first, or attach via the IPage constructor.");

    private void ClearHelpers()
    {
        _nativeTabs?.ClearDisplayNames();
        _nativeTabs = null;
        _automationTabs = null;
        _frames = null;
        _dialogs = null;
        _keyboard = null;
        _cookieJar = null;
        _headerStore = null;
    }

    private async Task CloseOwnedAsync()
    {
        try {
            if (Context != null)
                await Context.CloseAsync().ConfigureAwait(false);
        }
        catch {
            // best-effort
        }

        try {
            if (_browser != null) {
                await _browser.CloseAsync().ConfigureAwait(false);
                await _browser.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch {
            // best-effort
        }
    }

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
