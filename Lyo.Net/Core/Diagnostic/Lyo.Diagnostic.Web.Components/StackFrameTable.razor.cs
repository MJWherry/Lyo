using Lyo.Diagnostic.StackTrace;
using Microsoft.AspNetCore.Components;

namespace Lyo.Diagnostic.Web.Components;

public partial class StackFrameTable
{
    [Parameter]
    public IReadOnlyList<StackFrame> Frames { get; set; } = Array.Empty<StackFrame>();
}