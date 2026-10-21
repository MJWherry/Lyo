using Lyo.Endato.Client.Models.Person.Response;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonResult = Lyo.Endato.Client.Models.Person.Response.Person;

namespace Lyo.Endato.Web.Components;

public partial class EndatoPersonSearchResults
{
    [Parameter]
    public PersonQueryResponse? Response { get; set; }

    private async Task ShowPerson(PersonResult person)
    {
        var parameters = new DialogParameters<EndatoPersonView> { { d => d.Person, person } };
        await DialogService.ShowAsync<EndatoPersonView>(person.FullName, parameters, LyoDialogPresets.Large);
    }
}