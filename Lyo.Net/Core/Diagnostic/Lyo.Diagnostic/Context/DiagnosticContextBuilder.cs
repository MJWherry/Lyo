using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.StackTrace;
using Lyo.Exceptions;

namespace Lyo.Diagnostic.Context;

/// <summary>
/// Combines <see cref="IStackTraceDecoder" /> and <see cref="IExceptionClassifier" /> into a single structured <see cref="DiagnosticContext" />. Register as Scoped or
/// Singleton depending on whether you inject scoped services.
/// </summary>
public sealed class DiagnosticContextBuilder(IStackTraceDecoder decoder, IExceptionClassifier classifier, DiagnosticContextOptions? options = null) : IDiagnosticContextBuilder
{
    private readonly DiagnosticContextOptions _options = options ?? DiagnosticContextOptions.Default;

    /// <inheritdoc />
    public DiagnosticContext Build(Exception exception) => Build(exception, RequestMetadata.Empty);

    /// <inheritdoc />
    public DiagnosticContext Build(Exception exception, RequestMetadata request)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        var trace = decoder.Decode(exception);
        return Build(trace, exception, request);
    }

    /// <inheritdoc />
    public DiagnosticContext Build(DecodedStackTrace trace, Exception exception, RequestMetadata request)
    {
        ArgumentHelpers.ThrowIfNull(trace);
        ArgumentHelpers.ThrowIfNull(exception);
        ArgumentHelpers.ThrowIfNull(request);
        var classification = classifier.Classify(exception);
        return new(
            DateTimeOffset.UtcNow, _options.OccurrenceIdFactory(), trace, classification, request, trace.Fingerprint, _options.Environment, _options.ServiceName,
            _options.ServiceVersion);
    }

    /// <inheritdoc />
    public Task<DiagnosticContext> BuildAsync(Exception exception, CancellationToken ct = default)
        => BuildAsync(exception, RequestMetadata.Empty, ct);

    /// <inheritdoc />
    public async Task<DiagnosticContext> BuildAsync(Exception exception, RequestMetadata request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        ArgumentHelpers.ThrowIfNull(request);
        var trace = await decoder.DecodeAsync(exception, ct).ConfigureAwait(false);
        return Build(trace, exception, request);
    }
}