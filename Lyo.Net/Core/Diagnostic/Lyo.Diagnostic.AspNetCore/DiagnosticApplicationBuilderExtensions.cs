using Microsoft.AspNetCore.Builder;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Registers the exception-recording middleware.</summary>
public static class DiagnosticApplicationBuilderExtensions
{
    /// <summary>
    /// Writes unhandled exceptions to the inbox and structured logs, then rethrows. Call <b>after</b> outer catch-all middleware (for example register
    /// <c>UseMiddleware&lt;LoggingMiddleware&gt;()</c> first so it wraps this middleware and still produces the HTTP error body).
    /// </summary>
    public static IApplicationBuilder UseDiagnosticExceptionRecording(this IApplicationBuilder app) => app.UseMiddleware<DiagnosticExceptionRecordingMiddleware>();
}