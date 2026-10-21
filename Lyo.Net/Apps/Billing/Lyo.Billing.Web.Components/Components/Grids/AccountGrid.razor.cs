using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class AccountGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Code"), new("Name")];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;
}