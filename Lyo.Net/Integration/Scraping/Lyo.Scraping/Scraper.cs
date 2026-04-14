using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Resilience;
using Lyo.Scraping.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Lyo.Scraping;

/// <summary>Web scraper built on Selenium WebDriver with resilience, metrics, and control wrappers.</summary>
public class Scraper : IDisposable
{
    private readonly IResilientExecutor _executor;
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _metricNames;
    private readonly IMetrics _metrics;
    private readonly ScraperOptions _options;
    private bool _disposed;
    private ChromeDriver? _driver;

    /// <summary>Gets the underlying WebDriver when the browser is started.</summary>
    public IWebDriver? Driver => _driver;

    /// <summary>Creates a new scraper.</summary>
    /// <param name="options">Scraper options.</param>
    /// <param name="executor">Resilience executor for polling operations.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    public Scraper(ScraperOptions options, IResilientExecutor executor, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNull(executor, nameof(executor));
        _options = options;
        _executor = executor;
        _logger = logger ?? NullLogger.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = CreateMetricNamesDictionary();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _driver?.Quit();
        _driver?.Dispose();
        _driver = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>Starts the browser asynchronously.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartBrowserAsync(CancellationToken ct = default)
    {
        if (_driver != null) {
            _logger.LogDebug("Browser already started");
            return;
        }

        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StartBrowserDuration)]);
        await Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    var chromeOptions = new ChromeOptions();
                    foreach (var arg in _options.WebDriverArguments)
                        chromeOptions.AddArgument(arg);

                    if (_options.UserAgents.Count > 0) {
                        var ua = _options.UserAgents[new Random().Next(_options.UserAgents.Count)];
                        chromeOptions.AddArgument($"user-agent={ua}");
                    }

                    chromeOptions.AddArgument($"--window-size={_options.BrowserWindowWidth},{_options.BrowserWindowHeight}");
                    chromeOptions.AddAdditionalOption("goog:loggingPrefs", new Dictionary<string, object> { { "browser", "All" } });
                    _driver = new(chromeOptions);
                    _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_options.PageLoadTimeoutSeconds);
                    _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(_options.ImplicitWaitSeconds);
                    _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(_options.ScriptTimeoutSeconds);
                    _logger.LogInformation("Browser started");
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Stops the browser asynchronously.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopBrowserAsync(CancellationToken ct = default)
    {
        if (_driver == null) {
            _logger.LogDebug("Browser not running");
            return;
        }

        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.StopBrowserDuration)]);
        await Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    try {
                        _driver.Quit();
                        _logger.LogInformation("Browser stopped");
                    }
                    finally {
                        _driver.Dispose();
                        _driver = null;
                    }
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Switches to the tab at the specified index (0-based).</summary>
    /// <param name="index">Tab index.</param>
    public void SelectTab(int index)
    {
        EnsureDriver();
        var handles = _driver!.WindowHandles.ToList();
        OperationHelpers.ThrowIf(handles.Count == 0, "No browser tabs available.");
        ArgumentHelpers.ThrowIfNotInRange(index, 0, handles.Count - 1, nameof(index));
        _driver.SwitchTo().Window(handles[index]);
        _logger.LogDebug("Switched to tab {Index}", index);
    }

    /// <summary>Switches to the tab with the specified handle.</summary>
    /// <param name="handle">Window handle.</param>
    public void SelectTab(string handle)
    {
        EnsureDriver();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(handle, nameof(handle));
        _driver!.SwitchTo().Window(handle);
        _logger.LogDebug("Switched to tab {Handle}", handle);
    }

    /// <summary>Gets the current window handles (tab IDs).</summary>
    public IReadOnlyList<string> GetTabHandles()
    {
        EnsureDriver();
        return _driver!.WindowHandles.ToList();
    }

    /// <summary>Creates a WebDriverWait with the configured timeout.</summary>
    public WebDriverWait CreateWait() => new(_driver!, TimeSpan.FromSeconds(_options.SeleniumMaxWaitSeconds));

    /// <summary>Waits for an element using WebDriverWait (sync).</summary>
    /// <param name="by">Locator (e.g. By.Id, By.Name, By.XPath).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The element if found, or null if cancelled.</returns>
    public IWebElement? WaitFor(By by, CancellationToken ct = default)
    {
        EnsureDriver();
        var wait = CreateWait();
        try {
            return wait.Until(d => {
                ct.ThrowIfCancellationRequested();
                return d.FindElement(by);
            });
        }
        catch (WebDriverException ex) when (ex is NoSuchElementException or WebDriverTimeoutException) {
            _logger.LogDebug(ex, "Element not found: {Locator}", by);
            return null;
        }
        catch (OperationCanceledException) {
            return null;
        }
    }

    /// <summary>Waits for an element using WebDriverWait (async).</summary>
    public async Task<IWebElement?> WaitForAsync(By by, CancellationToken ct = default) => await Task.Run(() => WaitFor(by, ct), ct).ConfigureAwait(false);

    /// <summary>Polls for an element using resilience (sync).</summary>
    /// <param name="by">Locator (e.g. By.Id, By.Name, By.XPath).</param>
    /// <param name="pipelineName">Optional resilience pipeline name.</param>
    /// <returns>The element if found.</returns>
    public IWebElement PollFor(By by, string? pipelineName = null) => PollForAsync(by, pipelineName, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Polls for an element using resilience (async).</summary>
    /// <param name="by">Locator (e.g. By.Id, By.Name, By.XPath).</param>
    /// <param name="pipelineName">Optional resilience pipeline name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The element if found.</returns>
    public async Task<IWebElement> PollForAsync(By by, string? pipelineName = null, CancellationToken ct = default)
    {
        EnsureDriver();
        var pipeline = pipelineName ?? _options.PollingPipelineName ?? PipelineNames.Basic;
        var locatorDesc = by.ToString();
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.PollDuration)], [("locator", locatorDesc)]);
        try {
            var element = await _executor.ExecuteAsync(
                    pipeline, async ct => {
                        ct.ThrowIfCancellationRequested();
                        var el = WaitFor(by, ct);
                        if (el == null)
                            throw new RetryableResultException($"Element not found: {locatorDesc}");

                        return el;
                    }, ct)
                .ConfigureAwait(false);

            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollSuccess)], tags: [("locator", locatorDesc)]);
            _logger.LogDebug("Found element: {Locator}", locatorDesc);
            return element;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.PollFailure)], tags: [("locator", locatorDesc)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.PollDuration)], ex, [("locator", locatorDesc)]);
            _logger.LogWarning(ex, "Failed to find element after retries: {Locator}", locatorDesc);
            throw;
        }
    }

    /// <summary>Polls for an element and wraps it as the specified control type (sync).</summary>
    /// <typeparam name="T">Control type (ButtonControl, InputControl, SelectControl, or ScraperControl).</typeparam>
    public T PollFor<T>(By by, string? pipelineName = null)
        where T : ScraperControl
        => PollForAsync<T>(by, pipelineName, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Polls for an element and wraps it as the specified control type (async).</summary>
    /// <typeparam name="T">Control type (ButtonControl, InputControl, SelectControl, or ScraperControl).</typeparam>
    public async Task<T> PollForAsync<T>(By by, string? pipelineName = null, CancellationToken ct = default)
        where T : ScraperControl
    {
        var element = await PollForAsync(by, pipelineName, ct).ConfigureAwait(false);
        return (T)WrapElement(element, typeof(T));
    }

    /// <summary>Navigates to the specified URL.</summary>
    public void NavigateTo(string url)
    {
        EnsureDriver();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(url));
        _driver!.Navigate().GoToUrl(url);
        _logger.LogDebug("Navigated to {Url}", url);
    }

    private void EnsureDriver() => OperationHelpers.ThrowIfNull(_driver, "Browser not started. Call StartBrowserAsync first.");

    private static ScraperControl WrapElement(IWebElement element, Type controlType)
    {
        if (controlType == typeof(ButtonControl))
            return new ButtonControl(element);

        if (controlType == typeof(InputControl))
            return new InputControl(element);

        if (controlType == typeof(SelectControl))
            return new SelectControl(element);

        if (controlType == typeof(ScraperControl))
            return new(element);

        throw new ArgumentException($"Unknown control type: {controlType.Name}", nameof(controlType));
    }

    private static Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.StartBrowserDuration), Constants.Metrics.StartBrowserDuration },
            { nameof(Constants.Metrics.StopBrowserDuration), Constants.Metrics.StopBrowserDuration },
            { nameof(Constants.Metrics.PollSuccess), Constants.Metrics.PollSuccess },
            { nameof(Constants.Metrics.PollFailure), Constants.Metrics.PollFailure },
            { nameof(Constants.Metrics.PollDuration), Constants.Metrics.PollDuration }
        };
}