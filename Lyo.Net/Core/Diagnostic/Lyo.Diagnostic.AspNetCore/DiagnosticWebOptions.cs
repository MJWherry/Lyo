using Lyo.Common.Core.Net;
using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Inbox;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Host settings for breadcrumb capacity, inbox recording filters, and correlation headers.</summary>
public sealed class DiagnosticWebOptions
{
    /// <summary>Only occurrences at or above this severity are written to <see cref="IErrorOccurrenceSink" />.</summary>
    public ExceptionSeverity MinimumSeverity { get; set; } = ExceptionSeverity.Low;

    /// <summary>False skips occurrences whose <see cref="DiagnosticContext.IsExpectedControlFlow" /> flag is set.</summary>
    public bool RecordExpectedControlFlow { get; set; }

    /// <summary>How many breadcrumbs are kept for each HTTP request scope.</summary>
    public int BreadcrumbCapacity { get; set; } = 100;

    /// <summary>Ceiling for <see cref="InMemoryErrorInbox" /> when <see cref="DiagnosticWebServiceCollectionExtensions.AddLyoDiagnosticsWeb" /> registers it.</summary>
    public int InMemoryInboxMaxOccurrences { get; set; } = 5_000;

    /// <summary>
    /// Request headers tried in order for <see cref="RequestMetadata.CorrelationId" />; falls back to
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier" />.
    /// </summary>
    public string[] CorrelationIdHeaders { get; set; } = LyoHttpHeaders.CorrelationIds;
}