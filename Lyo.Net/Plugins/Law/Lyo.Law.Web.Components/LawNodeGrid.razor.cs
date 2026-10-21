using Lyo.Law.Models;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;

namespace Lyo.Law.Web.Components;

/// <summary>Projected statute grid. Body is not selected. Open a row to read and save it.</summary>
public partial class LawNodeGrid
{
    private static readonly string[] _keySelectFields = ["Id", "Citation", "Heading", "Label", "Kind", "IsRepealed", "Code.Key", "Code.Jurisdiction.Key", "Annotations.Count"];

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Code.Jurisdiction.Key", "State"), new("Code.Name", "Code"), new("Kind"), new("Label"), new("Citation"), new("Heading")
    ];

    private LyoDataGridProjected? _dataGrid;

    /// <summary>HTTP client used for QueryProject.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client used by the node dialog.</summary>
    [Parameter]
    [EditorRequired]
    public ILawClient LawClient { get; set; } = null!;

    /// <summary>Law route prefix.</summary>
    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Law.Route;

    private string _nodesRoute => $"{BaseRoute.TrimEnd('/')}/Nodes";

    private async Task OpenNode(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var heading = ProjectedValueHelper.GetDisplayValue(item, "Heading");
        var parameters = new DialogParameters<LawNodeView> { { d => d.LawClient, LawClient }, { d => d.NodeId, id } };
        var dialog = await DialogService.ShowAsync<LawNodeView>(string.IsNullOrWhiteSpace(heading) ? "Statute" : heading, parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }
}