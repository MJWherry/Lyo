using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Images;
using Lyo.Query.Models.Common.Request;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
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

public partial class EndatoPsPersonGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FirstName"), new("LastName")];

    private static readonly string[] Includes = ["Addresses", "EmailAddresses", "PhoneNumbers"];

    private async Task ViewPerson(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<EndatoPsPersonRes>>(
            $"{Lyo.TestGateway.Models.Constants.EndatoPs.Person}/QueryConcrete",
            new() { Keys = [[id]], Amount = 1, Include = [..Includes] });
        var person = res?.Items?.FirstOrDefault();
        if (person == null)
            return;

        var parameters = new DialogParameters<EndatoPsPersonView> { { d => d.Person, person } };
        await DialogService.ShowAsync<EndatoPsPersonView>(string.IsNullOrWhiteSpace(person.FullName) ? "Endato person" : person.FullName, parameters, LyoDialogPresets.Large);
    }
}
