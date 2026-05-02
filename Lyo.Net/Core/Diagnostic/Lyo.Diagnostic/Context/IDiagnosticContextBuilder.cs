using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Context;

public interface IDiagnosticContextBuilder
{
    /// <summary>Builds a full diagnostic context from a live exception, with no request metadata.</summary>
    DiagnosticContext Build(Exception exception);

    /// <summary>Builds a full diagnostic context from a live exception and request metadata.</summary>
    DiagnosticContext Build(Exception exception, RequestMetadata request);

    /// <summary>Builds from an already-decoded trace (avoids double-decoding in middleware).</summary>
    DiagnosticContext Build(DecodedStackTrace trace, Exception exception, RequestMetadata request);
}
