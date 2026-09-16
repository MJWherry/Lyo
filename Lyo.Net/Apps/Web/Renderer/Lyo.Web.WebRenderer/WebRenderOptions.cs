using Lyo.Exceptions.Models;
using PuppeteerSharp;

namespace Lyo.Web.WebRenderer;

/// <summary>Browser and metrics settings for <see cref="WebRendererService" />. Section name is <see cref="SectionName" />.</summary>
public sealed class WebRenderOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WebRenderOptions";

    /// <summary>Chromium or Chrome executable. Null skips browser checks so HTML-only hosts can boot.</summary>
    public string? BrowserExePath { get; set; } = Utilities.DetectBrowserPath(SupportedBrowser.Chrome);

    /// <summary>When true, and an <c>IMetrics</c> is registered, render and capture operations are recorded.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>How many captures may use the shared browser at once. Starts at 4.</summary>
    public int MaxConcurrentPages { get; set; } = 4;

    /// <summary>How long a page may take to load HTML and produce a capture. Starts at 30 seconds.</summary>
    public TimeSpan ContentTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Pass <c>--no-sandbox</c> to Chrome. Leave false outside containers.</summary>
    public bool NoSandbox { get; set; }

    /// <summary>Checks concurrency, timeout, and that a set <see cref="BrowserExePath" /> exists on disk.</summary>
    public void Validate()
    {
        if (MaxConcurrentPages < 1)
            throw new ConfigurationException("MaxConcurrentPages must be at least 1.", nameof(MaxConcurrentPages));

        if (ContentTimeout <= TimeSpan.Zero)
            throw new ConfigurationException("ContentTimeout must be positive.", nameof(ContentTimeout));

        if (!string.IsNullOrWhiteSpace(BrowserExePath) && !File.Exists(BrowserExePath))
            throw new ConfigurationException($"BrowserExePath '{BrowserExePath}' does not exist.", nameof(BrowserExePath));
    }
}