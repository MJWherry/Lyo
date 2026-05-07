namespace Lyo.Web.Automation.Abstractions;

/// <summary>Engine-neutral navigation for the active tab/window (URL transitions and reload).</summary>
public interface IWebAutomationNavigator
{
    Task NavigateAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Navigates to <paramref name="url" />, calling <paramref name="onRequest" /> with the URL of each outgoing network request observed before, during, and after the page
    /// load. Continues observing until <paramref name="onRequest" /> returns <see langword="true" /> (signalling the caller found what it needed) or <paramref name="ct" /> is cancelled.
    /// For Chromium-based Selenium sessions this requires performance logging to be enabled.
    /// </summary>
    Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default);

    /// <summary>Reloads the current page (same tab).</summary>
    Task ReloadAsync(CancellationToken ct = default);
}