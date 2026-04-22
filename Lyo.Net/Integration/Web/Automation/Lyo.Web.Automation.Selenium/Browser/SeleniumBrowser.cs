using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Selenium.Automation;
using Lyo.Web.Automation.Selenium.Configuration;
using Lyo.Web.Automation.Selenium.Controls;
using Lyo.Web.Automation.Selenium.Service;
using Lyo.Web.Automation.Selenium.WebDriver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Selenium WebDriver session façade: polling, metrics, controls, and <see cref="IWebAutomationBrowser" />.</summary>
public class SeleniumBrowser : IDisposable, IWebAutomationBrowser
{
    private readonly Dictionary<string, string> _metricNames;
    private BrowserAlerts? _alerts;
    private SeleniumCookieJar? _cookieJar;
    private bool _disposed;
    private FrameNavigator? _frames;
    private BrowserKeyboard? _keyboard;
    private TabManager? _tabs;

    /// <summary>Gets the underlying WebDriver when the browser is started.</summary>
    public IWebDriver? Driver { get; private set; }

    /// <summary>Correlation id when this browser was created from <see cref="ISeleniumBrowserService.CreateSession" />; otherwise <see cref="Guid.Empty" />.</summary>
    public Guid SessionId => ExecutionContext?.SessionId ?? Guid.Empty;

    /// <summary>Session-scoped paths and temp lifetime when created from <see cref="ISeleniumBrowserService.CreateSession" />.</summary>
    public SeleniumExecutionContext? ExecutionContext { get; }

    /// <summary>JavaScript alert / confirm / prompt helpers.</summary>
    public BrowserAlerts Alerts => _alerts ??= new(this);

    /// <summary>Iframe and nested browsing context switching.</summary>
    public FrameNavigator Frames => _frames ??= new(this);

    /// <summary>Keyboard input via Selenium <see cref="OpenQA.Selenium.Interactions.Actions" />.</summary>
    public BrowserKeyboard Keyboard => _keyboard ??= new(this);

    internal ILogger Logger { get; }

    internal IMetrics Metrics { get; }

    internal SeleniumBrowserOptions Options { get; }

    internal string SessionIdLabel => SessionId == Guid.Empty ? "none" : SessionId.ToString("N");

    /// <summary>Tab/window operations, metadata, open/close/switch. Valid after <see cref="StartBrowserAsync" />.</summary>
    public TabManager Tabs {
        get {
            EnsureDriver();
            return _tabs ??= new(this, Logger);
        }
    }

    /// <summary>Creates a new browser instance.</summary>
    /// <param name="options">Browser and polling options.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    public SeleniumBrowser(SeleniumBrowserOptions options, ILogger? logger = null, IMetrics? metrics = null)
        : this(options, null, logger, metrics) { }

    /// <summary>Creates a browser with optional session execution context (profile paths, temp session).</summary>
    public SeleniumBrowser(SeleniumBrowserOptions options, SeleniumExecutionContext? executionContext, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        Options = options;
        ExecutionContext = executionContext ?? SeleniumExecutionContextFactory.Create(options, Guid.NewGuid());
        var baseLogger = logger ?? NullLogger.Instance;
        Logger = ExecutionContext.BuildLogger(baseLogger);
        Metrics = Options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Driver?.Quit();
        _tabs?.ClearDisplayNames();
        _tabs = null;
        _alerts = null;
        _frames = null;
        _keyboard = null;
        _cookieJar = null;
        Driver?.Dispose();
        Driver = null;
        ExecutionContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public IBrowserCookies? CookieJar => Driver != null ? _cookieJar ??= new(this) : null;

    /// <inheritdoc />
    public IBrowserHeaders? ExtraHeaders => null;

    Task IWebAutomationBrowser.NavigateAsync(string url, CancellationToken ct) => NavigateToAsync(url, ct);

    async Task IWebAutomationBrowser.NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct)
    {
        await NavigateToAsync(url, ct).ConfigureAwait(false);
        while (!ct.IsCancellationRequested) {
            var done = await Task.Run(
                    () => {
                        try {
                            var logs = Driver!.Manage().Logs.GetLog(LogType.Performance);
                            foreach (var entry in logs) {
                                var reqUrl = TryParseNetworkRequestUrl(entry.Message);
                                if (reqUrl != null && onRequest(reqUrl))
                                    return true;
                            }
                        }
                        catch {
                            // performance log unavailable for this browser/driver combination
                        }

                        return false;
                    }, ct)
                .ConfigureAwait(false);

            if (done)
                return;

            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        ct.ThrowIfCancellationRequested();
    }

    Task<string> IWebAutomationBrowser.GetPageSourceAsync(CancellationToken ct)
        => Task.Run(
            () => {
                EnsureDriver();
                ct.ThrowIfCancellationRequested();
                return Driver!.PageSource;
            }, ct);

    Task IWebAutomationBrowser.ReloadAsync(CancellationToken ct)
        => Task.Run(
            () => {
                EnsureDriver();
                ct.ThrowIfCancellationRequested();
                Driver!.Navigate().Refresh();
            }, ct);

    Task<string> IWebAutomationBrowser.GetCurrentUrlAsync(CancellationToken ct)
        => Task.Run(
            () => {
                EnsureDriver();
                ct.ThrowIfCancellationRequested();
                return Driver!.Url;
            }, ct);

    Task<string> IWebAutomationBrowser.GetTitleAsync(CancellationToken ct)
        => Task.Run(
            () => {
                EnsureDriver();
                ct.ThrowIfCancellationRequested();
                return Driver!.Title;
            }, ct);

    async Task<IWebAutomationElement> IWebAutomationBrowser.PollForElementAsync(ElementLocatorChain chain, CancellationToken ct)
        => await PollForChainAsync(chain, ct).ConfigureAwait(false);

    Task<IReadOnlyList<IWebAutomationElement>> IWebAutomationBrowser.PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct) => PollForElementsAsync(chain, ct);

    Task<IWebAutomationElement?> IWebAutomationBrowser.GetElementAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementChainAsync(chain, ct);

    Task<IReadOnlyList<IWebAutomationElement>?> IWebAutomationBrowser.GetElementsAsync(ElementLocatorChain chain, CancellationToken ct) => GetElementsChainAsync(chain, ct);

    Task<byte[]> IWebAutomationBrowser.TakeViewportSnapshotPngAsync(CancellationToken ct) => TakeViewportSnapshotPngAsync(ct);

    /// <summary>Resolved metric name for members of <see cref="Web.Automation.Core.Constants.Metrics" /> (Selenium-specific series).</summary>
    internal string ResolveMetric(string metricMemberName) => _metricNames[metricMemberName];

    /// <summary>Begins a structured logging scope (<c>session_id</c>, <c>operation</c>, <c>tab</c>, <c>url</c>) for multi-step flows.</summary>
    /// <param name="urlOverride">When set, used for <c>url</c> instead of the current document URL.</param>
    public IDisposable? BeginOperationScope(string operation, string? urlOverride = null)
    {
        var url = urlOverride ?? TryGetCurrentUrl();
        return Logger.BeginScope(
            new Dictionary<string, object?> {
                ["session_id"] = SessionIdLabel,
                ["operation"] = operation,
                ["tab"] = TryCurrentWindowHandle(),
                ["url"] = url == null ? null : BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs)
            });
    }

    /// <summary>Throws if the browser is not started; returns the active WebDriver.</summary>
    internal IWebDriver GetRequiredDriver()
    {
        EnsureDriver();
        return Driver!;
    }

    /// <summary>Starts the browser asynchronously.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartBrowserAsync(CancellationToken ct = default)
    {
        if (Driver != null) {
            Logger.LogDebug("Browser already started");
            return;
        }

        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.StartBrowserDuration)], SeleniumMetricTags.ForOperation(this, "start_browser"));
        await Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    Driver = WebDriverFactory.CreateDriver(Options, ExecutionContext);
                    ApplyWindowSize(Driver);
                    Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(Options.PageLoadTimeoutSeconds);
                    Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(Options.ImplicitWaitSeconds);
                    Driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(Options.ScriptTimeoutSeconds);
                    Logger.LogInformation("Browser started ({BrowserKind})", Options.BrowserKind);
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Stops the browser asynchronously.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopBrowserAsync(CancellationToken ct = default)
    {
        if (Driver == null) {
            Logger.LogDebug("Browser not running");
            return;
        }

        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.StopBrowserDuration)], SeleniumMetricTags.ForOperation(this, "stop_browser"));
        await Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    try {
                        Driver!.Quit();
                        Logger.LogInformation("Browser stopped");
                    }
                    finally {
                        _tabs?.ClearDisplayNames();
                        _tabs = null;
                        _alerts = null;
                        _frames = null;
                        _keyboard = null;
                        Driver.Dispose();
                        Driver = null;
                    }
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Gets the current page URL.</summary>
    public string GetCurrentUrl()
    {
        EnsureDriver();
        return Driver!.Url;
    }

    /// <summary>Gets the current document title.</summary>
    public string GetTitle()
    {
        EnsureDriver();
        return Driver!.Title;
    }

    /// <summary>Gets the full HTML source of the current page.</summary>
    public string GetPageSource()
    {
        EnsureDriver();
        return Driver!.PageSource;
    }

    /// <summary>Scrolls the window by the given pixel offsets.</summary>
    public void ScrollBy(int xOffset, int yOffset)
    {
        EnsureDriver();
        GetJavaScriptExecutor().ExecuteScript("window.scrollBy(arguments[0], arguments[1]);", xOffset, yOffset);
    }

    /// <summary>Scrolls the window to the given document coordinates.</summary>
    public void ScrollTo(int x, int y)
    {
        EnsureDriver();
        GetJavaScriptExecutor().ExecuteScript("window.scrollTo(arguments[0], arguments[1]);", x, y);
    }

    /// <summary>Scrolls to the bottom of the page.</summary>
    public void ScrollToBottom()
    {
        EnsureDriver();
        GetJavaScriptExecutor().ExecuteScript("window.scrollTo(0, Math.max(document.body.scrollHeight, document.documentElement.scrollHeight));");
    }

    /// <summary>Scrolls to the top of the page.</summary>
    public void ScrollToTop()
    {
        EnsureDriver();
        GetJavaScriptExecutor().ExecuteScript("window.scrollTo(0, 0);");
    }

    /// <summary>Scrolls the given element into view.</summary>
    public void ScrollIntoView(IWebElement element)
    {
        EnsureDriver();
        ArgumentHelpers.ThrowIfNull(element, nameof(element));
        GetJavaScriptExecutor().ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", element);
    }

    /// <summary>Creates a WebDriverWait with the configured timeout.</summary>
    public WebDriverWait CreateWait() => new(Driver!, TimeSpan.FromSeconds(Options.SeleniumMaxWaitSeconds));

    /// <summary>Waits for an element using WebDriverWait (sync).</summary>
    /// <param name="by">Locator (e.g. By.Id, By.Name, By.XPath).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The element if found, or null if cancelled.</returns>
    public IWebElement? WaitFor(By by, CancellationToken ct = default)
    {
        EnsureDriver();
        return SeleniumPolling.TryWaitForElement(Driver!, by, Options.SeleniumMaxWaitSeconds, Logger, ct);
    }

    /// <summary>Waits for an element using WebDriverWait (async).</summary>
    public async Task<IWebElement?> WaitForAsync(By by, CancellationToken ct = default) => await Task.Run(() => WaitFor(by, ct), ct).ConfigureAwait(false);

    /// <summary>
    /// Retries <see cref="WaitFor" /> up to <see cref="SeleniumBrowserOptions.PollingMaxAttempts" /> with <see cref="SeleniumBrowserOptions.PollingDelayBetweenAttempts" />
    /// between attempts (sync).
    /// </summary>
    public IWebElement PollFor(By by) => PollForAsync(by, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// One <see cref="WaitFor" /> (bounded by <see cref="SeleniumBrowserOptions.SeleniumMaxWaitSeconds" />); does not apply outer
    /// <see cref="SeleniumBrowserOptions.PollingMaxAttempts" />. Returns <see langword="null" /> if not found.
    /// </summary>
    public async Task<IWebElement?> GetElementAsync(By by, CancellationToken ct = default)
    {
        EnsureDriver();
        return await Task.Run(() => WaitFor(by, ct), ct).ConfigureAwait(false);
    }

    /// <summary>Async variant of <see cref="PollFor" />; each attempt runs <see cref="GetElementAsync(OpenQA.Selenium.By,System.Threading.CancellationToken)" />.</summary>
    public async Task<IWebElement> PollForAsync(By by, CancellationToken ct = default)
    {
        EnsureDriver();
        var locatorDesc = by.ToString();
        var pollTags = SeleniumMetricTags.ForOperation(this, "poll", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var element = await GetElementAsync(by, ct).ConfigureAwait(false);
                if (element != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    Logger.LogDebug("Found element: {Locator}", locatorDesc);
                    return element;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Element not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            Logger.LogWarning(ex, "Failed to find element after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Polls for an element and wraps it as the specified control type (sync).</summary>
    /// <typeparam name="T">Control type (ButtonControl, InputControl, SelectControl, or WebElementControl).</typeparam>
    public T PollFor<T>(By by)
        where T : WebElementControl
        => PollForAsync<T>(by, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Polls for an element and wraps it as the specified control type (async).</summary>
    /// <typeparam name="T">Control type (ButtonControl, InputControl, SelectControl, or WebElementControl).</typeparam>
    public async Task<T> PollForAsync<T>(By by, CancellationToken ct = default)
        where T : WebElementControl
    {
        var element = await PollForAsync(by, ct).ConfigureAwait(false);
        return (T)WrapElement(element, typeof(T));
    }

    /// <summary>Navigates to the specified URL.</summary>
    public void NavigateTo(string url)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(url));
        EnsureDriver();
        Driver!.Navigate().GoToUrl(url);
        var forLog = BrowserUrlRedaction.ForLog(url, Options.MaskSensitiveUrlsInLogs);
        Logger.LogDebug("Navigated to {Url}", forLog);
    }

    /// <summary>Async navigation with cancellation.</summary>
    public Task NavigateToAsync(string url, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                NavigateTo(url);
            }, ct);

    /// <summary>Waits until <c>document.readyState === 'complete'</c> or the page load timeout elapses.</summary>
    public async Task WaitForDocumentReadyAsync(CancellationToken ct = default)
    {
        EnsureDriver();
        var executor = GetJavaScriptExecutor();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(Options.PageLoadTimeoutSeconds);
        while (DateTime.UtcNow < deadline) {
            ct.ThrowIfCancellationRequested();
            var state = executor.ExecuteScript("return document.readyState") as string;
            if (string.Equals(state, "complete", StringComparison.Ordinal))
                return;

            await Task.Delay(25, ct).ConfigureAwait(false);
        }

        throw new WebDriverException("document.readyState did not reach 'complete' within the configured page load timeout.");
    }

    internal string? TryGetCurrentUrl()
    {
        try {
            return Driver?.Url;
        }
        catch {
            return null;
        }
    }

    private string? TryCurrentWindowHandle()
    {
        try {
            return Driver?.CurrentWindowHandle;
        }
        catch {
            return null;
        }
    }

    private void ApplyWindowSize(IWebDriver driver)
    {
        try {
            driver.Manage().Window.Size = new(Options.BrowserWindowWidth, Options.BrowserWindowHeight);
        }
        catch {
            // headless / remote: best effort
        }
    }

    private void EnsureDriver() => OperationHelpers.ThrowIfNull(Driver, "Browser not started. Call StartBrowserAsync first.");

    private IJavaScriptExecutor GetJavaScriptExecutor() => Driver as IJavaScriptExecutor ?? throw new InvalidOperationException("WebDriver does not support JavaScript execution.");

    private static WebElementControl WrapElement(IWebElement element, Type controlType)
    {
        if (controlType == typeof(ButtonControl))
            return new ButtonControl(element);

        if (controlType == typeof(InputControl))
            return new InputControl(element);

        if (controlType == typeof(SelectControl))
            return new SelectControl(element);

        if (controlType == typeof(CheckboxControl))
            return new CheckboxControl(element);

        if (controlType == typeof(LinkControl))
            return new LinkControl(element);

        if (controlType == typeof(TextAreaControl))
            return new TextAreaControl(element);

        if (controlType == typeof(WebElementControl))
            return new(element);

        throw new ArgumentException($"Unknown control type: {controlType.Name}", nameof(controlType));
    }

    /// <summary>One nested wait per <see cref="SeleniumBrowserOptions.SeleniumMaxWaitSeconds" />; no outer polling retries. Returns <see langword="null" /> if not found.</summary>
    public async Task<IWebElement?> GetDescendantElementAsync(IWebElement parent, ElementLocator locator, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(parent, nameof(parent));
        var by = ElementLocatorMapping.ToBy(locator);
        return await TryGetNestedElementAsync(parent, by, ct).ConfigureAwait(false);
    }

    /// <summary>Polls for a descendant of <paramref name="parent" /> (nested <see cref="IWebElement.FindElement(OpenQA.Selenium.By)" /> with retries).</summary>
    public async Task<IWebElement> PollForDescendantElementAsync(IWebElement parent, ElementLocator locator, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(parent, nameof(parent));
        var by = ElementLocatorMapping.ToBy(locator);
        return await PollForNestedAsync(parent, by, $"{locator.Kind}:{locator.Value}", ct).ConfigureAwait(false);
    }

    /// <summary>Resolves a locator chain once (respects frame context if switched). Returns <see langword="null" /> if not found.</summary>
    public async Task<IWebAutomationElement?> GetElementChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        EnsureDriver();
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var getTags = SeleniumMetricTags.ForOperation(this, "get_chain", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], getTags);
        try {
            var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (el == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
                Logger.LogDebug("Chained element not found: {Locator}", locatorDesc);
                return null;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: getTags);
            Logger.LogDebug("Resolved chained element: {Locator}", locatorDesc);
            return new SeleniumWebAutomationElement(this, el);
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, getTags);
            Logger.LogWarning(ex, "Failed to resolve chained element: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Polls for a locator chain from the current document; each attempt runs <see cref="GetElementChainCoreAsync" />.</summary>
    public async Task<IWebAutomationElement> PollForChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        EnsureDriver();
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var pollTags = SeleniumMetricTags.ForOperation(this, "poll_chain", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await GetElementChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (el != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    Logger.LogDebug("Found chained element: {Locator}", locatorDesc);
                    return new SeleniumWebAutomationElement(this, el);
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Chained element not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            Logger.LogWarning(ex, "Failed to find chained element after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Single-attempt chain resolution (one bounded wait per segment). Returns <see langword="null" /> if any segment is not found.</summary>
    private async Task<IWebElement?> GetElementChainCoreAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        EnsureDriver();
        var driver = Driver!;
        IWebElement? current = null;
        for (var i = 0; i < segments.Count; i++) {
            ct.ThrowIfCancellationRequested();
            var by = ElementLocatorMapping.ToBy(segments[i]);
            if (i == 0) {
                current = await Task.Run(() => SeleniumPolling.TryWaitForElement(driver, by, Options.SeleniumMaxWaitSeconds, Logger, ct), ct).ConfigureAwait(false);
            }
            else {
                current = await Task.Run(() => SeleniumPolling.TryWaitForNestedElement(driver, current!, by, Options.SeleniumMaxWaitSeconds, Logger, ct), ct).ConfigureAwait(false);
            }

            if (current == null)
                return null;
        }

        return current;
    }

    /// <summary>Resolves the chain once and returns every match for the final segment. Returns <see langword="null" /> if not found.</summary>
    public async Task<IReadOnlyList<IWebAutomationElement>?> GetElementsChainAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        EnsureDriver();
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var getTags = SeleniumMetricTags.ForOperation(this, "get_many", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], getTags);
        try {
            var raw = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
            if (raw == null) {
                Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
                Logger.LogDebug("Elements not resolved: {Locator}", locatorDesc);
                return null;
            }

            var wrapped = raw.Select(el => (IWebAutomationElement)new SeleniumWebAutomationElement(this, el)).ToList();
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: getTags);
            Logger.LogDebug("Resolved {Count} elements: {Locator}", wrapped.Count, locatorDesc);
            return wrapped;
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: getTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, getTags);
            Logger.LogWarning(ex, "Failed to resolve elements: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Waits until at least one match exists, then returns every matching element for the final segment; each attempt runs <see cref="GetElementsChainCoreAsync" />.</summary>
    public async Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        EnsureDriver();
        var segments = chain.Segments;
        var locatorDesc = string.Join(" -> ", segments.Select(s => $"{s.Kind}:{s.Value}"));
        var pollTags = SeleniumMetricTags.ForOperation(this, "poll_many", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var raw = await GetElementsChainCoreAsync(segments, ct).ConfigureAwait(false);
                if (raw != null) {
                    var wrapped = raw.Select(el => (IWebAutomationElement)new SeleniumWebAutomationElement(this, el)).ToList();
                    Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    Logger.LogDebug("Found {Count} elements: {Locator}", wrapped.Count, locatorDesc);
                    return wrapped;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Elements not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            Logger.LogWarning(ex, "Failed to find elements after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Single-attempt multi-match resolution (one bounded wait per segment). Returns <see langword="null" /> if the chain cannot be resolved.</summary>
    private async Task<IReadOnlyList<IWebElement>?> GetElementsChainCoreAsync(IReadOnlyList<ElementLocator> segments, CancellationToken ct)
    {
        EnsureDriver();
        var driver = Driver!;
        if (segments.Count == 1) {
            var by = ElementLocatorMapping.ToBy(segments[0]);
            var first = await Task.Run(() => SeleniumPolling.TryWaitForElement(driver, by, Options.SeleniumMaxWaitSeconds, Logger, ct), ct).ConfigureAwait(false);
            if (first == null)
                return null;

            return driver.FindElements(by);
        }

        var parentPath = segments.Take(segments.Count - 1).ToArray();
        var parent = await GetElementChainCoreAsync(parentPath, ct).ConfigureAwait(false);
        if (parent == null)
            return null;

        var lastSeg = segments[segments.Count - 1];
        var byLast = ElementLocatorMapping.ToBy(lastSeg);
        var nested = await TryGetNestedElementAsync(parent, byLast, ct).ConfigureAwait(false);
        if (nested == null)
            return null;

        return parent.FindElements(byLast);
    }

    private async Task<IWebElement?> TryGetNestedElementAsync(IWebElement parent, By by, CancellationToken ct)
    {
        EnsureDriver();
        var driver = Driver!;
        return await Task.Run(() => SeleniumPolling.TryWaitForNestedElement(driver, parent, by, Options.SeleniumMaxWaitSeconds, Logger, ct), ct).ConfigureAwait(false);
    }

    private async Task<IWebElement> PollForNestedAsync(IWebElement parent, By by, string locatorDesc, CancellationToken ct)
    {
        EnsureDriver();
        var pollTags = SeleniumMetricTags.ForOperation(this, "poll_nested", new[] { ("locator", locatorDesc) });
        using var timer = Metrics.StartTimer(_metricNames[nameof(Wm.Metrics.PollDuration)], pollTags);
        try {
            var max = Math.Max(1, Options.PollingMaxAttempts);
            for (var attempt = 1; attempt <= max; attempt++) {
                ct.ThrowIfCancellationRequested();
                var el = await TryGetNestedElementAsync(parent, by, ct).ConfigureAwait(false);
                if (el != null) {
                    Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollSuccess)], tags: pollTags);
                    return el;
                }

                if (attempt < max)
                    await Task.Delay(Options.PollingDelayBetweenAttempts, ct).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Nested element not found after {max} attempts: {locatorDesc}");
        }
        catch (Exception ex) {
            Metrics.IncrementCounter(_metricNames[nameof(Wm.Metrics.PollFailure)], tags: pollTags);
            Metrics.RecordError(_metricNames[nameof(Wm.Metrics.PollDuration)], ex, pollTags);
            throw;
        }
    }

    private static Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Wm.Metrics.StartBrowserDuration), Wm.Metrics.StartBrowserDuration },
            { nameof(Wm.Metrics.StopBrowserDuration), Wm.Metrics.StopBrowserDuration },
            { nameof(Wm.Metrics.PollSuccess), Wm.Metrics.PollSuccess },
            { nameof(Wm.Metrics.PollFailure), Wm.Metrics.PollFailure },
            { nameof(Wm.Metrics.PollDuration), Wm.Metrics.PollDuration },
            { nameof(Wm.Metrics.TabOperationDuration), Wm.Metrics.TabOperationDuration },
            { nameof(Wm.Metrics.TabOperation), Wm.Metrics.TabOperation },
            { nameof(Wm.Metrics.FrameOperationDuration), Wm.Metrics.FrameOperationDuration },
            { nameof(Wm.Metrics.FrameOperation), Wm.Metrics.FrameOperation },
            { nameof(Wm.Metrics.AlertOperationDuration), Wm.Metrics.AlertOperationDuration },
            { nameof(Wm.Metrics.AlertOperation), Wm.Metrics.AlertOperation },
            { nameof(Wm.Metrics.KeyboardOperationDuration), Wm.Metrics.KeyboardOperationDuration },
            { nameof(Wm.Metrics.ControlInteraction), Wm.Metrics.ControlInteraction }
        };

    private static string? TryParseNetworkRequestUrl(string logMessage)
    {
        // Performance log entry shape: {"message":{"method":"Network.requestWillBeSent","params":{"request":{"url":"..."}}}}
        try {
            using var doc = JsonDocument.Parse(logMessage);
            var msg = doc.RootElement.GetProperty("message");
            if (!string.Equals(msg.GetProperty("method").GetString(), "Network.requestWillBeSent", StringComparison.Ordinal))
                return null;

            return msg.GetProperty("params").GetProperty("request").GetProperty("url").GetString();
        }
        catch {
            return null;
        }
    }

    /// <summary>Captures the visible viewport of the active window as PNG.</summary>
    public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                var driver = GetRequiredDriver();
                ct.ThrowIfCancellationRequested();
                if (driver is not ITakesScreenshot shot)
                    throw new NotSupportedException("WebDriver does not support screenshots.");

                return shot.GetScreenshot().AsByteArray;
            }, ct);

    private sealed class SeleniumCookieJar : IBrowserCookies
    {
        private readonly SeleniumBrowser _browser;

        internal SeleniumCookieJar(SeleniumBrowser browser) => _browser = browser;

        public Task<IReadOnlyList<BrowserCookie>> GetCookiesAsync(string? url = null, CancellationToken ct = default)
            => Task.Run(
                () => {
                    var driver = _browser.GetRequiredDriver();
                    ct.ThrowIfCancellationRequested();
                    IReadOnlyList<BrowserCookie> result = driver.Manage()
                        .Cookies.AllCookies.Select(c => new BrowserCookie {
                            Name = c.Name,
                            Value = c.Value,
                            Domain = c.Domain,
                            Path = c.Path,
                            Secure = c.Secure,
                            HttpOnly = c.IsHttpOnly,
                            Expiry = c.Expiry.HasValue ? new DateTimeOffset(c.Expiry.Value) : null
                        })
                        .ToList();

                    return result;
                }, ct);

        public Task AddCookiesAsync(IEnumerable<BrowserCookie> cookies, CancellationToken ct = default)
            => Task.Run(
                () => {
                    var driver = _browser.GetRequiredDriver();
                    ct.ThrowIfCancellationRequested();
                    var jar = driver.Manage().Cookies;
                    foreach (var c in cookies)
                        jar.AddCookie(new(c.Name, c.Value, c.Domain ?? "", c.Path ?? "/", c.Expiry?.UtcDateTime));
                }, ct);

        public Task ClearCookiesAsync(CancellationToken ct = default)
            => Task.Run(
                () => {
                    var driver = _browser.GetRequiredDriver();
                    ct.ThrowIfCancellationRequested();
                    driver.Manage().Cookies.DeleteAllCookies();
                }, ct);
    }
}