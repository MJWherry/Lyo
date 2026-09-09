using System.Net.Http.Json;
using Lyo.Diagnostic.StackTrace;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

public partial class StackFrameTable
{
    [Parameter]
    public IReadOnlyList<StackFrame> Frames { get; set; } = Array.Empty<StackFrame>();
}
