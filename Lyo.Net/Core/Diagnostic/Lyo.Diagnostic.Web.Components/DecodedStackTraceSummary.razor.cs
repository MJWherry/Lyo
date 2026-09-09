using System.Net.Http.Json;
using Lyo.Diagnostic.StackTrace;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

public partial class DecodedStackTraceSummary
{
    [Parameter]
    public DecodedStackTrace? Trace { get; set; }
}
