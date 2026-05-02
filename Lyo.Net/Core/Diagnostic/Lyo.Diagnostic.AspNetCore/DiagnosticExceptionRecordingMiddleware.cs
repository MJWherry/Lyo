using Lyo.Diagnostic.Breadcrumbs;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Inbox;
using Lyo.Diagnostic.Logging;
using Lyo.Diagnostic.Sanitisation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>
/// Catches exceptions from later middleware, records <see cref="ErrorOccurrenceRecord" />, optionally logs via <see cref="IStructuredLogEnricher" />, then rethrows.
/// Register <b>after</b> outer middleware such as <c>LoggingMiddleware</c> in the pipeline so this sits closer to route handlers and runs first on unwind.
/// </summary>
public sealed class DiagnosticExceptionRecordingMiddleware(RequestDelegate next)
{
    /// <summary>Middleware entry point.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        IDiagnosticContextBuilder diagnosticBuilder,
        IBreadcrumbTrail breadcrumbTrail,
        IErrorOccurrenceSink errorSink,
        ITraceSanitiser sanitiser,
        IStructuredLogEnricher structuredLog,
        IOptions<DiagnosticWebOptions> options,
        ILogger<DiagnosticExceptionRecordingMiddleware> logger)
    {
        try {
            await next(context);
        }
        catch (Exception ex) {
            await TryRecordAndLogAsync(ex);
            throw;
        }

        async ValueTask TryRecordAndLogAsync(Exception ex)
        {
            var opts = options.Value;
            RequestMetadata requestMeta;
            try {
                requestMeta = context.ToDiagnosticRequestMetadata(opts);
            }
            catch (Exception mapEx) {
                logger.LogWarning(mapEx, "Failed to build diagnostic request metadata");
                requestMeta = RequestMetadata.Empty;
            }

            DiagnosticContext diagnostic;
            try {
                diagnostic = diagnosticBuilder.Build(ex, requestMeta);
            }
            catch (Exception buildEx) {
                logger.LogWarning(buildEx, "Failed to build diagnostic context");
                return;
            }

            if (!opts.RecordExpectedControlFlow && diagnostic.IsExpectedControlFlow)
                return;

            if (!ErrorOccurrenceMapper.MeetsMinimumSeverity(diagnostic.Classification.Severity, opts.MinimumSeverity))
                return;

            IReadOnlyList<Breadcrumb> crumbs;
            try {
                crumbs = breadcrumbTrail.Snapshot();
            }
            catch (Exception trailEx) {
                logger.LogWarning(trailEx, "Failed to snapshot breadcrumb trail");
                crumbs = [];
            }

            ErrorOccurrenceRecord record;
            try {
                record = ErrorOccurrenceMapper.FromDiagnosticContext(diagnostic, crumbs, sanitiser);
            }
            catch (Exception mapEx) {
                logger.LogWarning(mapEx, "Failed to map error occurrence record");
                return;
            }

            try {
                await errorSink.RecordAsync(record, context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception sinkEx) {
                logger.LogWarning(sinkEx, "Failed to record error occurrence {OccurrenceId}", diagnostic.OccurrenceId);
            }

            try {
                structuredLog.Log(logger, diagnostic);
            }
            catch (Exception logEx) {
                logger.LogWarning(logEx, "Failed to emit structured diagnostic log for {OccurrenceId}", diagnostic.OccurrenceId);
            }
        }
    }
}
