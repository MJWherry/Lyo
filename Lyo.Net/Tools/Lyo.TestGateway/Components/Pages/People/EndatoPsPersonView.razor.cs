using System.Net.Http.Json;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class EndatoPsPersonView
{
    [Parameter]
    [EditorRequired]
    public EndatoPsPersonRes Person { get; set; } = null!;

    private IReadOnlyList<string> PhoneTypes => PersonViewHelpers.PhoneTypeOptions(null, Person.PhoneNumbers.Select(p => p.Type));
}
