using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Sanitisation;

public interface ITraceSanitiser
{
    /// <summary>Sanitises a decoded stack trace according to configured options.</summary>
    SanitisedStackTrace Sanitise(DecodedStackTrace trace);

    /// <summary>Sanitises the trace inside a <see cref="DiagnosticContext" />.</summary>
    SanitisedStackTrace Sanitise(DiagnosticContext context);
}