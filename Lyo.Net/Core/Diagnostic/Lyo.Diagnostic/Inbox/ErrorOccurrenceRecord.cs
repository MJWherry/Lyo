using System.Diagnostics;
using Lyo.Diagnostic.Breadcrumbs;
using Lyo.Diagnostic.Classification;

namespace Lyo.Diagnostic.Inbox;

/// <summary>One recorded exception occurrence for inbox storage or export.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ErrorOccurrenceRecord(
    string OccurrenceId,
    string Fingerprint,
    string ExceptionKind,
    string? ServiceName,
    ExceptionSeverity Severity,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    string? CrashLocation,
    string? ExceptionMessage,
    string? Environment,
    int BreadcrumbCount,
    IReadOnlyList<Breadcrumb>? BreadcrumbsSnapshot)
{
    /// <summary>Builds the canonical grouping key for this occurrence.</summary>
    public ErrorGroupKey GroupKey => new(Fingerprint, ExceptionKind, ServiceName);

    public override string ToString() => $"{OccurrenceId} {ExceptionKind} fp={Fingerprint} @ {OccurredAt:u}";
}