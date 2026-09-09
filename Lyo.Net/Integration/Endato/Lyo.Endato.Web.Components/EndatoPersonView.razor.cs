using Lyo.Endato.Client.Models.Enrichment.Response;
using Lyo.Endato.Client.Models.Person.Response;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using PersonResult = Lyo.Endato.Client.Models.Person.Response.Person;

namespace Lyo.Endato.Web.Components;

public partial class EndatoPersonView
{
    /// <summary>Person Search result shown in the UI.</summary>
    [Parameter]
    [EditorRequired]
    public PersonResult Person { get; set; } = null!;

    private IReadOnlyList<(string Label, int Count)> Indicators => EndatoViewHelpers.ActiveIndicators(Person.Indicators);

    private int AssociateCount => Person.AssociateSummaries is { Count: > 0 } summaries ? summaries.Count : Person.Associates?.Count ?? 0;

    private int AkaCount => (Person.Akas?.Count ?? 0) + (Person.MergedNames?.Count ?? 0);

    private IReadOnlyList<string> PhoneTypes => EndatoViewHelpers.PhoneTypeOptions(null, Person.PhoneNumbers?.Select(p => p.PhoneType) ?? []);

    private string LocationText
        => Person.Locations is { Count: > 0 } ? string.Join("; ", Person.Locations.Select(l => $"{l.City}, {l.State}")) : "";

    private async Task ShowPersonJson()
    {
        var parameters = new DialogParameters<JsonViewDialog<PersonResult>> { { i => i.Data, Person } };
        await DialogService.ShowAsync(typeof(JsonViewDialog<PersonResult>), Person.FullName, parameters, LyoDialogPresets.Medium);
    }
}
