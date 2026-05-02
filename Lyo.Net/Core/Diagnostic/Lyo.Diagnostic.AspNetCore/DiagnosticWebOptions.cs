using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Inbox;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Host wiring for breadcrumb capacity, inbox recording filters, and correlation headers.</summary>
public sealed class DiagnosticWebOptions
{
    /// <summary>Only occurrences at or above this severity are written to <see cref="IErrorOccurrenceSink" />.</summary>
    public ExceptionSeverity MinimumSeverity { get; set; } = ExceptionSeverity.Low;

    /// <summary>When false, occurrences with <see cref="DiagnosticContext.IsExpectedControlFlow" /> are not recorded.</summary>
    public bool RecordExpectedControlFlow { get; set; }

    /// <summary>Maximum breadcrumbs retained per HTTP request scope.</summary>
    public int BreadcrumbCapacity { get; set; } = 100;

    /// <summary>Cap for <see cref="InMemoryErrorInbox" /> when registered by <see cref="DiagnosticWebServiceCollectionExtensions.AddLyoDiagnosticsWeb" />.</summary>
    public int InMemoryInboxMaxOccurrences { get; set; } = 5_000;

    /// <summary>Request headers tried in order for <see cref="RequestMetadata.CorrelationId" />; falls back to <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier" />.</summary>
    public string[] CorrelationIdHeaders { get; set; } = ["X-Correlation-Id", "X-Request-Id"];
}
