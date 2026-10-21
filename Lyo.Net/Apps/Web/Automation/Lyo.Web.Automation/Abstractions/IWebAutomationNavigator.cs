namespace Lyo.Web.Automation.Abstractions;

/// <summary>Engine-neutral navigation for the active tab/window (URL transitions, reload, and a timed network listen).</summary>
public interface IWebAutomationNavigator
{
    Task NavigateAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Navigates to <paramref name="url" />, calling <paramref name="onRequest" /> with the URL of each outgoing network request observed before, during, and after the page
    /// load. Continues observing until <paramref name="onRequest" /> yields <see langword="true" /> (signalling the caller found what it needed) or <paramref name="ct" /> is cancelled.
    /// For Chromium-based Selenium sessions this requires performance logging to be enabled.
    /// </summary>
    Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default);

    /// <summary>Reloads the current page (same tab).</summary>
    Task ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// Listens for outgoing request URLs for <paramref name="durationMilliseconds" /> and returns the distinct matches. A null or blank <paramref name="urlPattern" /> keeps
    /// every URL. Otherwise the pattern is a regular expression, matched without case. Chromium Selenium sessions need performance logging enabled.
    /// </summary>
    Task<IReadOnlyList<string>> CaptureNetworkAsync(string? urlPattern, int durationMilliseconds, CancellationToken ct = default);
}