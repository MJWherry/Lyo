using Lyo.Common.Metadata.Records;
using Lyo.Query.Models.Builders;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Config.Web.Components;

public partial class ConfigManagement
{
    /// <summary>
    /// Store used for all definition, binding, resolve, and revision writes. Required. Hosts inject the store (in-process Postgres or HTTP <c>ConfigApiStore</c>) and pass it in.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>
    /// API client behind the Lyo.Query grids (search/filter/sort/export), so definition reads hit the API instead of the local store. Writes still go through
    /// <see cref="Store" /> — add <c>ConfigApiStore</c> on the workbench so writes also reach the API (encryption runs there).
    /// </summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Starting subject entity type. Defaults to <see cref="AppConfigEntity.AppEntityType" />.</summary>
    [Parameter]
    public string InitialSubjectEntityType { get; set; } = AppConfigEntity.AppEntityType;

    /// <summary>Starting subject entity id (string; app routes use <c>kind:id</c>).</summary>
    [Parameter]
    public string? InitialSubjectEntityId { get; set; }

    private string _typeDraft = AppConfigEntity.AppEntityType;
    private string _idDraft = "";
    private string _subjectEntityType = AppConfigEntity.AppEntityType;
    private string _subjectEntityId = "";
    private int _tabIndex;
    private int _dataVersion;
    private Guid? _selectedBindingId;
    private Guid? _selectedDefinitionId;
    private bool _initialized;

    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _typeDraft = string.IsNullOrWhiteSpace(InitialSubjectEntityType) ? AppConfigEntity.AppEntityType : InitialSubjectEntityType.Trim();
        _idDraft = InitialSubjectEntityId?.Trim() ?? "";
        _subjectEntityType = _typeDraft;
        _subjectEntityId = _idDraft;
        _initialized = true;
    }

    private void ApplyScope()
    {
        var type = _typeDraft.Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            Snackbar.Add("Subject entity type is required.", Severity.Warning);
            return;
        }

        _subjectEntityType = type;
        _subjectEntityId = _idDraft.Trim();
        _dataVersion++;
    }

    private void Refresh() => _dataVersion++;

    private void HandleViewDefinitionHistory(Guid definitionId)
    {
        _selectedDefinitionId = definitionId;
        _tabIndex = 2;
        _dataVersion++;
    }

    private void HandleViewValueHistory(Guid bindingId)
    {
        _selectedBindingId = bindingId;
        _tabIndex = 3;
        _dataVersion++;
    }

    private void OnToolbarKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            ApplyScope();
    }
}
