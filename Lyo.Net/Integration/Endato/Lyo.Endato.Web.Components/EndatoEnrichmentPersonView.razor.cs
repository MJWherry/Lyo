using Lyo.Endato.Client.Models.Enrichment.Response;
using Lyo.Endato.Client.Models.Person.Response;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using EnrichmentPerson = Lyo.Endato.Client.Models.Enrichment.Response.Person;

namespace Lyo.Endato.Web.Components;

public partial class EndatoEnrichmentPersonView
{
    /// <summary>Enrichment person shown in the UI.</summary>
    [Parameter]
    [EditorRequired]
    public EnrichmentPerson Person { get; set; } = null!;

    /// <summary>Identity score on the enrichment envelope.</summary>
    [Parameter]
    public int IdentityScore { get; set; }

    /// <summary>full enrichment response for the View JSON action. May be omitted.</summary>
    [Parameter]
    public EnrichmentResponse? Response { get; set; }

    private IReadOnlyList<string> PhoneTypes => EndatoViewHelpers.PhoneTypeOptions(null, Person.Phones.Select(p => p.Type));

    private string TitleName
    {
        get {
            var name = string.Join(" ", new[] { Person.Name.FirstName, Person.Name.MiddleName, Person.Name.LastName }.Where(static s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(name) ? "Enriched person" : name;
        }
    }

    private async Task ShowResponseJson()
    {
        if (Response is null)
            return;

        var parameters = new DialogParameters<JsonViewDialog<EnrichmentResponse>> { { i => i.Data, Response } };
        await DialogService.ShowAsync(typeof(JsonViewDialog<EnrichmentResponse>), "Enrichment Response", parameters, LyoDialogPresets.Medium);
    }
}
