using PuppeteerSharp;

namespace Lyo.Web.WebRenderer;

public sealed class WebRenderOptions
{
    public const string SectionName = "WebRenderOptions";

    public string? BrowserExePath { get; set; } = Utilities.DetectBrowserPath(SupportedBrowser.Chrome);

    public bool EnableMetrics { get; set; } = false;

    /// <summary>No required settings; present so DI can run the same Validate step as other options types.</summary>
    public void Validate()
    {
    }
}