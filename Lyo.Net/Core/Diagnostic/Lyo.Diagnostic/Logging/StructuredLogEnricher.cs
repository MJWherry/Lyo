using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Diagnostic.Logging;

/// <summary>
/// Writes structured diagnostic properties derived from a <see cref="DiagnosticContext" /> to Microsoft.Extensions.Logging. Serilog integration: pass the dictionary from
/// <see cref="BuildLogProperties" /> into a <c>LogContext.PushProperty</c> / <c>ForContext</c> call before logging, or destructure directly with <c>{@DiagContext}</c> in your message
/// template.
/// </summary>
public sealed class StructuredLogEnricher(LogEnricherOptions? options = null) : IStructuredLogEnricher
{
    private readonly LogEnricherOptions _options = options ?? LogEnricherOptions.Default;

    /// <inheritdoc />
    public void Log(ILogger logger, DiagnosticContext context, string? additionalMessage = null)
    {
        ArgumentHelpers.ThrowIfNull(logger);
        ArgumentHelpers.ThrowIfNull(context);
        var level = context.Classification.Severity switch {
            ExceptionSeverity.Low => LogLevel.Information,
            ExceptionSeverity.Medium => LogLevel.Warning,
            ExceptionSeverity.High => LogLevel.Error,
            ExceptionSeverity.Critical => LogLevel.Critical,
            var _ => LogLevel.Error
        };

        var props = BuildLogProperties(context);

        // Push all structured properties into a log scope so they appear in structured sinks (Seq, Elastic, Application Insights, etc.)
        using (logger.BeginScope(new Dictionary<string, object?>((IDictionary<string, object?>)props))) {
            var message = string.IsNullOrWhiteSpace(additionalMessage)
                ? "{ExceptionKind} in {ServiceName}: {ExceptionMessage}"
                : "{ExceptionKind} in {ServiceName}: {ExceptionMessage} — {AdditionalMessage}";

            logger.Log(level, message, context.Classification.Kind, context.ServiceName ?? "unknown", context.Trace.ExceptionMessage, additionalMessage);
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> BuildLogProperties(DiagnosticContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var props = new Dictionary<string, object?> {
            // Occurrence identity
            ["diag.OccurrenceId"] = context.OccurrenceId,
            ["diag.Fingerprint"] = context.Fingerprint,
            ["diag.OccurredAt"] = context.OccurredAt,

            // Service metadata
            ["diag.ServiceName"] = context.ServiceName,
            ["diag.ServiceVersion"] = context.ServiceVersion,
            ["diag.Environment"] = context.Environment,

            // Classification
            ["diag.ExceptionKind"] = context.Classification.Kind.ToString(),
            ["diag.ExceptionSeverity"] = context.Classification.Severity.ToString(),
            ["diag.ExceptionLabel"] = context.Classification.Label,
            ["diag.RemediationHint"] = context.Classification.RemediationHint,
            ["diag.IsExpectedControlFlow"] = context.Classification.IsExpectedControlFlow,

            // Stack trace summary
            ["diag.ExceptionMessage"] = context.Trace.ExceptionMessage,
            ["diag.CrashSite"] = context.Trace.LikelyCrashSite?.LocationSummary,
            ["diag.CrashSiteMethod"] = context.Trace.LikelyCrashSite?.ShortMethod,
            ["diag.CrashConfidence"] = context.Trace.CrashSiteConfidence.ToString(),
            ["diag.TotalFrames"] = context.Trace.TotalFrameCount,
            ["diag.UserFrames"] = context.Trace.UserFrameCount,
            ["diag.HasRecursion"] = context.Trace.HasRecursion,
            ["diag.InnerExceptionDepth"] = context.Trace.InnerExceptionDepth,
            ["diag.UserNamespaces"] = string.Join(", ", context.Trace.UserNamespaces),

            // Request metadata
            ["diag.CorrelationId"] = context.Request.CorrelationId,
            ["diag.HttpMethod"] = context.Request.HttpMethod,
            ["diag.Path"] = context.Request.Path,
            ["diag.UserIdentity"] = context.Request.UserIdentity,
            ["diag.ClientIp"] = context.Request.ClientIp
        };

        if (context.Trace.HasRecursion) {
            var top = context.Trace.RecursionPatterns[0];
            props["diag.RecursionDepth"] = top.Depth;
            props["diag.RecursionMethod"] = top.Frame.ShortMethod;
        }

        if (_options.IncludeAllFrames) {
            props["diag.Frames"] = context.Trace.AllFrames.Select(f => new {
                    f.ShortMethod,
                    f.LocationSummary,
                    Category = f.Category.ToString(),
                    f.IsAsync,
                    f.IsLambda
                })
                .ToList();
        }

        if (_options.IncludeInnerExceptions && context.Trace.InnerExceptions.Count > 0) {
            var depth = Math.Min(context.Trace.InnerExceptions.Count, _options.MaxInnerExceptionDepth);
            props["diag.InnerExceptions"] = context.Trace.InnerExceptions.Take(depth)
                .Select((inner, i) => new {
                    Index = i + 1,
                    inner.ExceptionMessage,
                    CrashSite = inner.LikelyCrashSite?.LocationSummary,
                    inner.Fingerprint
                })
                .ToList();
        }

        foreach (var i in context.Request.AdditionalProperties)
            props[$"diag.req.{i.Key}"] = i.Value;

        return props;
    }
}