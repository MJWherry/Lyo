using Lyo.Diagnostic.StackTrace;
using Microsoft.AspNetCore.Components;

namespace Lyo.Diagnostic.Web.Components;

public partial class DecodedStackTraceSummary
{
    [Parameter]
    public DecodedStackTrace? Trace { get; set; }
}