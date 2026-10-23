using Lyo.TestGateway.Models;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using MudBlazor;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class PersonGrid2
{
    private readonly IEnumerable<string> _includes = ["contactaddresses.address", "contactphonenumbers.phonenumber", "contactemailaddresses.emailaddress"];
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FirstName")];

    private LyoDataGrid<PersonRes>? _dataGrid;

    private async Task OpenEditor(PersonRes? person)
    {
        if (person == null)
            return;

        var id = ProjectedValueHelper.GetValue(person, "Id");
        if (id == null)
            return;

        var personRes = await ApiClient.GetAsAsync<PersonRes>($"{Models.Constants.Person.Route}/{id}");
        if (personRes == null)
            return;

        var dialogParameters = new DialogParameters<PersonForm> { { i => i.Person, personRes } };
        var dialog = await DialogService.ShowAsync<PersonForm>($"Edit {personRes.FullName}", dialogParameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (!result.Canceled && _dataGrid is not null)
            await _dataGrid.RefreshData();
    }
}