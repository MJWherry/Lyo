using Lyo.Diagnostic.Sanitisation;
using Microsoft.AspNetCore.Components;

namespace Lyo.Diagnostic.Web.Components;

public partial class SanitisedStackTracePanel
{
    [Parameter]
    public SanitisedStackTrace? Trace { get; set; }
}