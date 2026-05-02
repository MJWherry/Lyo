using Lyo.Exceptions;
using Lyo.Web.Automation.Selenium.WebDriver;

namespace Lyo.Web.Automation.Selenium.Configuration;

/// <summary>Fluent builder for <see cref="SeleniumBrowserOptions" /> (chain <see cref="Headless" />, <see cref="WindowSize" />, <see cref="Home" />, etc.).</summary>
public sealed class SeleniumBrowserOptionsBuilder
{
    private readonly SeleniumBrowserOptions _options = new();

    /// <summary>Starts an empty builder (same defaults as <see cref="SeleniumBrowserOptions" />).</summary>
    public static SeleniumBrowserOptionsBuilder New() => new();

    /// <summary>Returns a deep copy of the configured options (safe to mutate independently).</summary>
    public SeleniumBrowserOptions Build() => _options.Clone();

    /// <inheritdoc cref="SeleniumBrowserOptions.BrowserKind" />
    public SeleniumBrowserOptionsBuilder BrowserKind(SeleniumBrowserKind kind)
    {
        _options.BrowserKind = kind;
        return this;
    }

    /// <summary>Uses Chrome.</summary>
    public SeleniumBrowserOptionsBuilder Chrome() => BrowserKind(SeleniumBrowserKind.Chrome);

    /// <summary>Uses Edge.</summary>
    public SeleniumBrowserOptionsBuilder Edge() => BrowserKind(SeleniumBrowserKind.Edge);

    /// <summary>Uses Firefox.</summary>
    public SeleniumBrowserOptionsBuilder Firefox() => BrowserKind(SeleniumBrowserKind.Firefox);

    /// <inheritdoc cref="SeleniumBrowserOptions.Headless" />
    public SeleniumBrowserOptionsBuilder Headless(bool enabled = true)
    {
        _options.Headless = enabled;
        return this;
    }

    /// <summary>Runs a visible browser window (not headless).</summary>
    public SeleniumBrowserOptionsBuilder Headful()
    {
        _options.Headless = false;
        return this;
    }

    /// <summary>Same as <see cref="Headful" /> — visible browser at your machine (non-headless).</summary>
    public SeleniumBrowserOptionsBuilder Home() => Headful();

    /// <summary>Sets <see cref="SeleniumBrowserOptions.BrowserWindowWidth" /> and <see cref="SeleniumBrowserOptions.BrowserWindowHeight" />.</summary>
    public SeleniumBrowserOptionsBuilder WindowSize(int width, int height)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(width);
        ArgumentHelpers.ThrowIfNegativeOrZero(height);
        _options.BrowserWindowWidth = width;
        _options.BrowserWindowHeight = height;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.BrowserWindowWidth" />
    public SeleniumBrowserOptionsBuilder WindowWidth(int width)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(width);
        _options.BrowserWindowWidth = width;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.BrowserWindowHeight" />
    public SeleniumBrowserOptionsBuilder WindowHeight(int height)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(height);
        _options.BrowserWindowHeight = height;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.RemoteWebDriverUri" />
    public SeleniumBrowserOptionsBuilder RemoteWebDriver(string uri)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri);
        _options.RemoteWebDriverUri = uri;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.MaskSensitiveUrlsInLogs" />
    public SeleniumBrowserOptionsBuilder MaskSensitiveUrls(bool enabled = true)
    {
        _options.MaskSensitiveUrlsInLogs = enabled;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.ServiceRootDirectory" />
    public SeleniumBrowserOptionsBuilder ServiceRootDirectory(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        _options.ServiceRootDirectory = path;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.BrowserUserDataDirectory" />
    public SeleniumBrowserOptionsBuilder BrowserUserDataDirectory(string? path)
    {
        _options.BrowserUserDataDirectory = path;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.DownloadDirectory" />
    public SeleniumBrowserOptionsBuilder DownloadDirectory(string? path)
    {
        _options.DownloadDirectory = path;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.ArtifactsDirectory" />
    public SeleniumBrowserOptionsBuilder ArtifactsDirectory(string? path)
    {
        _options.ArtifactsDirectory = path;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.PageLoadTimeoutSeconds" />
    public SeleniumBrowserOptionsBuilder PageLoadTimeoutSeconds(int seconds)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(seconds);
        _options.PageLoadTimeoutSeconds = seconds;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.ImplicitWaitSeconds" />
    public SeleniumBrowserOptionsBuilder ImplicitWaitSeconds(int seconds)
    {
        ArgumentHelpers.ThrowIfNegative(seconds);
        _options.ImplicitWaitSeconds = seconds;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.ScriptTimeoutSeconds" />
    public SeleniumBrowserOptionsBuilder ScriptTimeoutSeconds(int seconds)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(seconds);
        _options.ScriptTimeoutSeconds = seconds;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.SeleniumMaxWaitSeconds" />
    public SeleniumBrowserOptionsBuilder SeleniumMaxWaitSeconds(int seconds)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(seconds);
        _options.SeleniumMaxWaitSeconds = seconds;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.EnableMetrics" />
    public SeleniumBrowserOptionsBuilder EnableMetrics(bool enabled = true)
    {
        _options.EnableMetrics = enabled;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.PollingMaxAttempts" />
    public SeleniumBrowserOptionsBuilder PollingMaxAttempts(int attempts)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(attempts);
        _options.PollingMaxAttempts = attempts;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.PollingDelayBetweenAttempts" />
    public SeleniumBrowserOptionsBuilder PollingDelayBetweenAttempts(TimeSpan delay)
    {
        ArgumentHelpers.ThrowIfNotInRange(delay, TimeSpan.Zero, null, nameof(delay));
        _options.PollingDelayBetweenAttempts = delay;
        return this;
    }

    /// <inheritdoc cref="SeleniumBrowserOptions.AddArgument" />
    public SeleniumBrowserOptionsBuilder AddArgument(string key, string? value = null)
    {
        _options.AddArgument(key, value);
        return this;
    }

    /// <summary>Appends a user agent string (in addition to the default rotating list).</summary>
    public SeleniumBrowserOptionsBuilder AddUserAgent(string userAgent)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(userAgent);
        _options.UserAgents.Add(userAgent.Trim());
        return this;
    }
}