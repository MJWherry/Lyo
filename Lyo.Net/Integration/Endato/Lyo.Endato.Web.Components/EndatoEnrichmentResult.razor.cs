using Lyo.Endato.Client.Models.Enrichment.Response;
using Lyo.Endato.Client.Models.Person.Response;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Endato.Web.Components;

public partial class EndatoEnrichmentResult
{
    [Parameter]
    public EnrichmentResponse? Response { get; set; }

    private async Task ShowPerson()
    {
        if (Response?.Person is null)
            return;

        var parameters = new DialogParameters<EndatoEnrichmentPersonView> {
            { d => d.Person, Response.Person },
            { d => d.IdentityScore, Response.IdentityScore },
            { d => d.Response, Response }
        };
        await DialogService.ShowAsync<EndatoEnrichmentPersonView>("Enriched person", parameters, LyoDialogPresets.Large);
    }
}
