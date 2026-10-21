using Lyo.Web.Automation.Playwright.Configuration;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>How a recording session starts. <see cref="Headless" /> defaults to false so the browser window is visible.</summary>
public sealed class PlaywrightAutomationStudioOptions
{
    /// <summary>Optional page opened when the session starts. Becomes the first navigate step.</summary>
    public string? StartUrl { get; set; }

    /// <summary>Display name stored on the plan.</summary>
    public string? PlanName { get; set; }

    /// <summary>Directory the drawer Save button writes into. Defaults to a <c>plans</c> folder under the current directory.</summary>
    public string OutputDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "plans");

    /// <summary>Plan JSON loaded into the Play tab when the window opens. Recording still starts empty.</summary>
    public string? PlanPath { get; set; }

    /// <summary>Launch without a window. Tests set this; a person recording leaves it false.</summary>
    public bool Headless { get; set; }

    /// <summary>Extra browser settings applied after <see cref="Headless" />.</summary>
    public Action<PlaywrightBrowserOptions>? ConfigureBrowser { get; set; }
}