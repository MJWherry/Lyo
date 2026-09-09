using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Images;
using Lyo.Job.Web.Components;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
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

public partial class PersonView
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Person record shown on the page and used as the patch target.</summary>
    [Parameter]
    [EditorRequired]
    public PersonRes Person { get; set; } = null!;

    private LyoForm<EditModel>? _form;
    private EditModel? _edit;
    private bool _hasChanges;
    private bool _saving;

    private string TitleName
    {
        get {
            if (_edit == null)
                return string.IsNullOrWhiteSpace(Person.FullName.Trim()) ? "Person" : Person.FullName.Trim();

            var name = string.Join(" ", new[] { _edit.NamePrefix, _edit.FirstName, _edit.MiddleName, _edit.LastName, _edit.NameSuffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(name) ? "Person" : name;
        }
    }

    private IReadOnlyList<PersonAddressRes> Addresses => Person.Addresses ?? [];

    private IReadOnlyList<PersonPhoneNumberRes> Phones => Person.PhoneNumbers ?? [];

    private IReadOnlyList<PersonEmailAddressRes> Emails => Person.EmailAddresses ?? [];

    private IReadOnlyList<string> PhoneTypes => PersonViewHelpers.PhoneTypeOptions(null, Phones.Select(p => p.Type));

    private sealed class EditModel
    {
        public Guid Id { get; set; }

        public Guid? EndatoPersonId { get; set; }

        public string? SourceEntityType { get; set; }

        public string? NamePrefix { get; set; }

        public string? FirstName { get; set; }

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public string? NameSuffix { get; set; }
    }

    protected override void OnParametersSet()
    {
        if (_edit != null)
            return;

        _edit = new() {
            Id = Person.Id,
            EndatoPersonId = Person.EndatoPersonId,
            SourceEntityType = Person.SourceEntityType,
            NamePrefix = Person.NamePrefix,
            FirstName = Person.FirstName,
            MiddleName = Person.MiddleName,
            LastName = Person.LastName,
            NameSuffix = Person.NameSuffix
        };
    }

    private async Task Save()
    {
        if (_form == null)
            return;

        var patch = _form.BuildPatchRequest(Person.Id);
        if (patch == null) {
            Snackbar.Add("No changes to save", Severity.Info);
            return;
        }

        _saving = true;
        try {
            var result = await ApiClient.PatchAsAsync<PatchRequest, PatchResult<PersonRes>>(Lyo.TestGateway.Models.Constants.Person.Route, patch);
            if (!result.IsSuccess) {
                Snackbar.Add(result.Error?.ToString() ?? "Patch failed.", Severity.Error);
                return;
            }

            Snackbar.Add("Person saved", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally {
            _saving = false;
        }
    }
}
