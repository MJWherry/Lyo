using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Images;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.Web.Components.Form;
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

public partial class PersonForm
{
    [Parameter]
    public PersonRes? Person { get; set; }

    private PersonRes? CurrentPerson { get; set; }

    private LyoForm<PersonRes>? _formRef;

    private static readonly HashSet<string> AllowedPersonPatchProperties = [nameof(PersonRes.EndatoPersonId), nameof(PersonRes.NamePrefix), nameof(PersonRes.FirstName), nameof(PersonRes.MiddleName), nameof(PersonRes.LastName), nameof(PersonRes.NameSuffix), nameof(PersonRes.SourceEntityType)];

    protected override void OnParametersSet()
    {
        if (Person is null) {
            CurrentPerson = null;
            return;
        }

        if (CurrentPerson is null || CurrentPerson.Id != Person.Id)
            CurrentPerson = Person;
    }

    private async Task HandleSaveChanges(LyoForm<PersonRes>.SubmitContext context)
    {
        if (CurrentPerson is null)
            return;

        foreach (var operation in context.Operations)
            await operation.Operation(CurrentPerson);

        if (!context.PropertyChanges.Any()) {
            Snackbar.Add("No person changes to save.", Severity.Info);
            return;
        }

        var patchBuilder = PatchRequestBuilder.ForId(CurrentPerson.Id);
        var includedCount = 0;
        foreach (var change in context.PropertyChanges.Values.Where(i => i.HasChanged)) {
            if (!AllowedPersonPatchProperties.Contains(change.PropertyName))
                continue;

            patchBuilder.SetProperty(change.PropertyName, change.CurrentValue);
            includedCount++;
        }

        if (includedCount == 0) {
            Snackbar.Add("No supported person fields changed.", Severity.Info);
            return;
        }

        var patchRequest = patchBuilder.Build();
        var patchResult = await ApiClient.PatchAsAsync<PatchRequest, PatchResult<PersonRes>>(Lyo.TestGateway.Models.Constants.Person.Route, patchRequest);
        if (!patchResult.IsSuccess) {
            Snackbar.Add(patchResult.Error?.ToString() ?? "Patch failed.", Severity.Error);
            return;
        }

        _formRef?.ResetChanges();
        Snackbar.Add("Person updated.", Severity.Success);
    }
}
