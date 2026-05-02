using Lyo.Diagnostic.Breadcrumbs;
using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Sanitisation;

namespace Lyo.Diagnostic.Inbox;

/// <summary>Maps <see cref="DiagnosticContext" /> plus optional breadcrumbs into <see cref="ErrorOccurrenceRecord" />.</summary>
public static class ErrorOccurrenceMapper
{
    /// <summary>Maximum characters stored for <see cref="ErrorOccurrenceRecord.ExceptionMessage" />.</summary>
    public const int DefaultMaxExceptionMessageLength = 2_048;

    /// <summary>Maximum breadcrumbs copied into <see cref="ErrorOccurrenceRecord.BreadcrumbsSnapshot" />.</summary>
    public const int DefaultMaxBreadcrumbsInSnapshot = 50;

    /// <summary>
    /// Builds a record from a diagnostic context. When <paramref name="sanitiser" /> is provided, crash site and exception message come from the sanitised trace.
    /// </summary>
    public static ErrorOccurrenceRecord FromDiagnosticContext(
        DiagnosticContext context,
        IReadOnlyList<Breadcrumb>? breadcrumbs = null,
        ITraceSanitiser? sanitiser = null,
        int maxExceptionMessageLength = DefaultMaxExceptionMessageLength,
        int maxBreadcrumbsInSnapshot = DefaultMaxBreadcrumbsInSnapshot)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        string? crashLocation;
        string? exceptionMessage;

        if (sanitiser is not null) {
            var st = sanitiser.Sanitise(context);
            crashLocation = st.CrashSite;
            exceptionMessage = Truncate(st.ExceptionMessage, maxExceptionMessageLength);
        }
        else {
            crashLocation = context.CrashLocation;
            exceptionMessage = Truncate(context.Trace.ExceptionMessage, maxExceptionMessageLength);
        }

        var snap = SnapshotBreadcrumbs(breadcrumbs, maxBreadcrumbsInSnapshot);

        return new(
            context.OccurrenceId,
            context.Fingerprint,
            context.Classification.Kind.ToString(),
            context.ServiceName,
            context.Classification.Severity,
            context.OccurredAt,
            context.Request.CorrelationId,
            crashLocation,
            exceptionMessage,
            context.Environment,
            breadcrumbs?.Count ?? 0,
            snap);
    }

    private static IReadOnlyList<Breadcrumb>? SnapshotBreadcrumbs(IReadOnlyList<Breadcrumb>? breadcrumbs, int maxCount)
    {
        if (breadcrumbs is null || breadcrumbs.Count == 0)
            return null;

        if (breadcrumbs.Count <= maxCount)
            return breadcrumbs;

        var list = new List<Breadcrumb>(maxCount);
        var skip = breadcrumbs.Count - maxCount;
        for (var i = skip; i < breadcrumbs.Count; i++)
            list.Add(breadcrumbs[i]);
        return list;
    }

    private static string? Truncate(string? s, int max)
    {
        if (s is null || s.Length == 0)
            return s;
        return s.Length <= max ? s : s.Substring(0, max);
    }

    /// <summary>Returns true when <paramref name="severity" /> is at least <paramref name="minimum" />.</summary>
    public static bool MeetsMinimumSeverity(ExceptionSeverity severity, ExceptionSeverity minimum)
        => (int)severity >= (int)minimum;
}
