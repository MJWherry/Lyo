using Lyo.Api.Client;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class JournalGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions =
    [
        new("Memo"),
        FilterPropertyDefinition.FromEnum("Status", BillingColorHelper.EnumFilterValues<LedgerJournalStatus>())
    ];

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;
}
