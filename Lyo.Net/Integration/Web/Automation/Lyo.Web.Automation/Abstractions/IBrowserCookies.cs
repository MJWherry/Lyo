using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>
/// Cookie access for browser engines that support it. Consumers should check <c>browser is IBrowserCookies cookies</c> before using these members and gracefully degrade when
/// the browser does not implement this interface.
/// </summary>
public interface IBrowserCookies
{
    /// <summary>Returns all cookies visible to the current session, optionally filtered to those matching <paramref name="url" />. Pass <see langword="null" /> to return every cookie.</summary>
    Task<IReadOnlyList<BrowserCookie>> GetCookiesAsync(string? url = null, CancellationToken ct = default);

    /// <summary>Adds or replaces cookies in the current session.</summary>
    Task AddCookiesAsync(IEnumerable<BrowserCookie> cookies, CancellationToken ct = default);

    /// <summary>Removes all cookies from the current session.</summary>
    Task ClearCookiesAsync(CancellationToken ct = default);
}