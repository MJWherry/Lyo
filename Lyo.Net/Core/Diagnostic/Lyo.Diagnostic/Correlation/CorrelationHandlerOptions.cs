using System;

namespace Lyo.Diagnostic.Correlation;

/// <summary>
/// Tunables for <see cref="LyoCorrelationDelegatingHandler"/>. The same defaults are used by <c>Lyo.Diagnostic.AspNetCore.DiagnosticWebOptions.CorrelationIdHeaders</c> so the inbound
/// reader and the outbound writer agree without extra configuration.
/// </summary>
public sealed class CorrelationHandlerOptions
{
    /// <summary>Default header order (mirrors <c>DiagnosticWebOptions.CorrelationIdHeaders</c>).</summary>
    public static readonly string[] DefaultHeaders = ["X-Correlation-Id", "X-Request-Id"];

    /// <summary>
    /// Header names checked on the outbound request. If any are already populated by the caller (e.g. via the typed-client's <c>before:</c> hook), the handler does nothing. Defaults
    /// to <see cref="DefaultHeaders"/>.
    /// </summary>
    public string[] DetectHeaderNames { get; set; } = DefaultHeaders;

    /// <summary>
    /// Header names written to the outbound request when no existing header is detected. The handler writes one header per entry, so a single resolved id can be advertised under
    /// multiple aliases simultaneously (e.g. <c>X-Correlation-Id</c> for Lyo services and <c>X-Request-Id</c> for legacy ones). Defaults to <see cref="DefaultHeaders"/>.
    /// </summary>
    public string[] WriteHeaderNames { get; set; } = DefaultHeaders;
}
