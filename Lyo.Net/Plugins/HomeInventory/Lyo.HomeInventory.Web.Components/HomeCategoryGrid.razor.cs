using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Category grid with name-then-add and inline rename.</summary>
public partial class HomeCategoryGrid
{
    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    private string _route => $"{BaseRoute.TrimEnd('/')}/Categories";

    private LyoDataGridProjected? _dataGrid;
    private string _draftName = "";
    private Guid? _editingId;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("Slug")];

    private void BeginEdit(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;
        _editingId = id;
        _draftName = ProjectedValueHelper.GetDisplayValue(item, "Name");
    }

    private void CancelEdit()
    {
        _editingId = null;
        _draftName = "";
    }

    private async Task AddOrUpdate()
    {
        var name = _draftName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_editingId is Guid id) {
            var loaded = await ApiClient.QueryConcreteAsync<QueryRes<HomeCategoryRes>>(_route, new QueryConcreteReq { Keys = [[id]], Amount = 1 });
            var existing = loaded?.Items?.FirstOrDefault();
            var req = existing == null
                ? new HomeCategoryReq { Name = name }
                : new HomeCategoryReq {
                    ParentCategoryId = existing.ParentCategoryId,
                    Name = name,
                    Slug = existing.Slug,
                    Description = existing.Description,
                    SortOrder = existing.SortOrder
                };
            await ApiClient.PostAsAsync<UpdateRequest<HomeCategoryReq>, UpdateResult<HomeCategoryRes>>($"{_route}/Update", new(req, id));
        }
        else {
            await ApiClient.PostAsAsync<HomeCategoryReq, CreateResult<HomeCategoryRes>>(_route, new HomeCategoryReq { Name = name });
        }

        _editingId = null;
        _draftName = "";
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }
}
