namespace Lyo.Recipe.Web.Components;

/// <summary>Cooking-unit catalog. Typed abbreviations stay on ingredient lines even after a unit row is deleted.</summary>
public partial class RecipeUnitGrid
{
    private string _abbreviation = "";
    private Guid? _editingId;
    private int _editingSort;
    private string _name = "";

    private List<UnitRes> _units = [];

    /// <summary>Typed client for the unit routes.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload() => _units = (await RecipeClient.ListUnitsAsync()).ToList();

    private void BeginEdit(UnitRes unit)
    {
        _editingId = unit.Id;
        _name = unit.Name;
        _abbreviation = unit.Abbreviation;
        _editingSort = unit.SortOrder;
    }

    private void Cancel()
    {
        _editingId = null;
        _name = "";
        _abbreviation = "";
        _editingSort = _units.Count;
    }

    private async Task Save()
    {
        var name = _name.Trim();
        var abbreviation = _abbreviation.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbreviation))
            return;

        try {
            await RecipeClient.SaveUnitAsync(
                new() {
                    Id = _editingId ?? Guid.Empty,
                    Name = name,
                    Abbreviation = abbreviation,
                    SortOrder = _editingId is null ? _units.Count : _editingSort
                });

            Cancel();
            await Reload();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task Delete(UnitRes unit)
    {
        await RecipeClient.DeleteUnitAsync(unit.Id);
        if (_editingId == unit.Id)
            Cancel();

        await Reload();
    }
}