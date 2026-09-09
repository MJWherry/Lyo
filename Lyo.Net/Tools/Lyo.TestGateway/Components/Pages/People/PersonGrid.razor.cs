using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Metadata.Records;
using Lyo.Images;
using Lyo.Query.Models.Builders;
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

public partial class PersonGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FirstName"), FilterPropertyDefinition.FromDatabase("LastName", "last_name", "person", "people")];

    private LyoDataGridProjected? _dataGrid;

    private static readonly string[] PersonIncludes = ["ContactAddresses.Address", "ContactPhoneNumbers.PhoneNumber", "ContactEmailAddresses.EmailAddress"];

    private static void AddMostRecentAddressSelects(ProjectionQueryReqBuilder query)
        => query.AddSelects("ContactAddresses.Address.UpdatedTimestamp", "ContactAddresses.Address.CreatedTimestamp");

    private static string? MostRecentAddressFullAddress(object? item)
    {
        var raw = ProjectedValueHelper.GetValue(item, "ContactAddresses.Address") ?? ProjectedValueHelper.GetValue(item, "ContactAddresses");
        return Rows(raw)
            .Select(row => RelatedProjection.Deserialize<PersonAddressRes>(ProjectedValueHelper.GetValue(row, "Address") ?? row))
            .OfType<PersonAddressRes>()
            .MaxBy(a => a.UpdatedTimestamp ?? a.CreatedTimestamp)
            ?.FullAddress;
    }

    private static IEnumerable<object?> Rows(object? raw)
        => raw switch {
            JsonElement { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().Select(static e => (object?)e),
            JsonElement je => [je],
            var _ => TypeConversion.ToEnumerable(raw)
        };

    private async Task OpenPerson(object? person)
    {
        var personRes = await LoadPerson(person);
        if (personRes == null)
            return;

        var parameters = new DialogParameters<PersonView> { { d => d.Person, personRes } };
        var dialog = await DialogService.ShowAsync<PersonView>(personRes.FullName, parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid is not null)
            await _dataGrid.RefreshData();
    }

    private async Task<PersonRes?> LoadPerson(object? person)
    {
        if (person == null)
            return null;

        var id = ProjectedValueHelper.GetValue(person, "Id");
        if (id == null)
            return null;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<PersonRes>>(
            $"{Lyo.TestGateway.Models.Constants.Person.Route}/QueryConcrete",
            new() { Keys = [[id]], Amount = 1, Include = [..PersonIncludes] });
        return res?.Items?.FirstOrDefault() ?? await ApiClient.GetAsAsync<PersonRes>($"{Lyo.TestGateway.Models.Constants.Person.Route}/{id}");
    }
}
