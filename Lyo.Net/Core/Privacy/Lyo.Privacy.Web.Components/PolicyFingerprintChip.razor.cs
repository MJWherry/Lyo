using System.Net.Http.Json;
using Lyo.Privacy.Policy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class PolicyFingerprintChip
{
    [Parameter]
    public RedactionPolicy? Policy { get; set; }
}
