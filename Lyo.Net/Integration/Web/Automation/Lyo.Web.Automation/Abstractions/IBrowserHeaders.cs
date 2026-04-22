using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>
/// Extra HTTP request header management for browser engines that support it (e.g. Playwright).
/// Headers set here are sent with every subsequent request made by the browser.
/// Consumers should check <c>browser is IBrowserHeaders hdrs</c> before using these members.
/// </summary>
public interface IBrowserHeaders
{
    /// <summary>
    /// Merges <paramref name="headers" /> into the session's extra headers.
    /// Replaces any existing header with the same name (case-insensitive).
    /// </summary>
    Task SetExtraHeadersAsync(IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Removes all extra headers previously set via <see cref="SetExtraHeadersAsync" />.</summary>
    Task ClearExtraHeadersAsync(CancellationToken ct = default);
}
