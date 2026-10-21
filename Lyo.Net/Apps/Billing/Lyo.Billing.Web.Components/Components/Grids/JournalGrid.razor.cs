using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class JournalGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Memo"), FilterPropertyDefinition.FromEnum("Status", BillingColorHelper.EnumFilterValues<LedgerJournalStatus>())
    ];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;
}