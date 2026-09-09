using System.Net.Http.Json;
using Lyo.Diagnostic.Sanitisation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

public partial class SanitisedStackTracePanel
{
    [Parameter]
    public SanitisedStackTrace? Trace { get; set; }
}
