using Lyo.Diagnostic.Sanitisation;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Web.Components;

public partial class DiagnosticWorkbench
{
    private readonly ITraceSanitiser _sanitiser = new TraceSanitiser();

    private DecodedStackTrace? _decoded;

    private SanitisedStackTrace? _sanitised;

    private Task OnTraceDecodedAsync(DecodedStackTrace trace)
    {
        _decoded = trace;
        _sanitised = _sanitiser.Sanitise(trace);
        StateHasChanged();
        return Task.CompletedTask;
    }
}