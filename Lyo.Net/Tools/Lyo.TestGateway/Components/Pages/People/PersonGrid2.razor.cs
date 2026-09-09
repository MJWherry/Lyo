using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Images;
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

public partial class PersonGrid2
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FirstName")];

    private readonly IEnumerable<string> _includes = ["contactaddresses.address", "contactphonenumbers.phonenumber", "contactemailaddresses.emailaddress"];

    private LyoDataGrid<PersonRes>? _dataGrid;

    private async Task OpenEditor(PersonRes? person)
    {
        if (person == null)
            return;

        var id = ProjectedValueHelper.GetValue(person, "Id");
        if (id == null)
            return;

        var personRes = await ApiClient.GetAsAsync<PersonRes>($"{Lyo.TestGateway.Models.Constants.Person.Route}/{id}");
        if (personRes == null)
            return;

        var dialogParameters = new DialogParameters<PersonForm> { { i => i.Person, personRes } };
        var dialog = await DialogService.ShowAsync<PersonForm>($"Edit {personRes.FullName}", dialogParameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (!result.Canceled && _dataGrid is not null)
            await _dataGrid.RefreshData();
    }
}
