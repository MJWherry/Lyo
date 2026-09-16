using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;

namespace Lyo.Recipe.Web.Components;

/// <summary>Category grid with name-then-add and inline rename.</summary>
public partial class RecipeCategoryGrid
{
    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Recipe route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Recipe";

    private string _route => $"{BaseRoute.TrimEnd('/')}/Categories";

    private LyoDataGridProjected? _dataGrid;
    private string _draftName = "";
    private Guid? _draftParentId;
    private Guid? _editingId;
    private List<RecipeCategoryRes> _categories = [];

    private IEnumerable<RecipeCategoryRes> ParentChoices
        => _categories.Where(c => _editingId is not Guid id || c.Id != id);

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("Slug")];

    private async Task BeginEdit(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;
        await LoadCategories();
        _editingId = id;
        _draftName = ProjectedValueHelper.GetDisplayValue(item, "Name");
        var loaded = await ApiClient.QueryConcreteAsync<QueryRes<RecipeCategoryRes>>(_route, new QueryConcreteReq { Keys = [[id]], Amount = 1 });
        var existing = loaded?.Items?.FirstOrDefault();
        if (existing != null) {
            _draftName = existing.Name;
            _draftParentId = existing.ParentId;
        }
    }

    private void CancelEdit()
    {
        _editingId = null;
        _draftName = "";
        _draftParentId = null;
    }

    private async Task AddOrUpdate()
    {
        var name = _draftName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_editingId is Guid id) {
            var loaded = await ApiClient.QueryConcreteAsync<QueryRes<RecipeCategoryRes>>(_route, new QueryConcreteReq { Keys = [[id]], Amount = 1 });
            var existing = loaded?.Items?.FirstOrDefault();
            var req = existing == null
                ? new RecipeCategoryReq { Name = name, Slug = RecipeSlugs.FromName(name), ParentId = _draftParentId }
                : new RecipeCategoryReq {
                    ParentId = _draftParentId,
                    Name = name,
                    Slug = existing.Slug
                };
            await ApiClient.PostAsAsync<UpdateRequest<RecipeCategoryReq>, UpdateResult<RecipeCategoryRes>>($"{_route}/Update", new(req, id));
        }
        else {
            await ApiClient.PostAsAsync<RecipeCategoryReq, CreateResult<RecipeCategoryRes>>(
                _route, new RecipeCategoryReq { Name = name, Slug = RecipeSlugs.FromName(name), ParentId = _draftParentId });
        }

        _editingId = null;
        _draftName = "";
        _draftParentId = null;
        await LoadCategories();
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task LoadCategories()
    {
        var res = await ApiClient.QueryConcreteAsync<QueryRes<RecipeCategoryRes>>(_route, new QueryConcreteReq { Amount = 500 });
        _categories = res?.Items?.ToList() ?? [];
    }

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => LoadCategories();
}
