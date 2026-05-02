using System.Diagnostics;
using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Context;

/// <summary>The fully enriched diagnostic payload produced for one exception occurrence. Safe to serialise and return in an API error response (after sanitisation).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagnosticContext(
    DateTimeOffset OccurredAt,
    string OccurrenceId,
    DecodedStackTrace Trace,
    ClassifiedExceptionResult Classification,
    RequestMetadata Request,
    string Fingerprint,
    string? Environment,
    string? ServiceName,
    string? ServiceVersion)
{
    /// <summary>True when this exception is expected control flow (e.g. cancellation) and likely does not represent a bug.</summary>
    public bool IsExpectedControlFlow => Classification.IsExpectedControlFlow;

    /// <summary>True when the decoder has high confidence in the crash site location.</summary>
    public bool HasHighConfidenceCrashSite => Trace.CrashSiteConfidence == CrashSiteConfidence.High;

    /// <summary>Shortcut to the most likely crash location summary, e.g. "OrderService.cs:87".</summary>
    public string? CrashLocation => Trace.LikelyCrashSite?.LocationSummary;

    public override string ToString() => $"{Classification.Kind} [{Classification.Severity}] {ServiceName ?? "?"} @ {OccurredAt:u}";
}