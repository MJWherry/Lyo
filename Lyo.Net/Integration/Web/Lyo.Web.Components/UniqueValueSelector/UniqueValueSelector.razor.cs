using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Web.Components.UniqueValueSelector;

public partial class UniqueValueSelector<T>
{
    private List<SpUniqueValueCount> _filteredItems = new();

    private string _searchText = string.Empty;
    private HashSet<string> _selectedValues = [];

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<SpUniqueValueCount> Items { get; set; } = [];

    [Parameter]
    public IEnumerable<string> SelectedValues { get; set; } = new List<string>();

    [Parameter]
    public EventCallback<IEnumerable<string?>> SelectedValuesChanged { get; set; }

    [Parameter]
    public EventCallback<string> SearchTextChanged { get; set; }

    [Parameter]
    public Func<T?, string>? ValueFormatter { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    private string SearchText {
        get => _searchText;
        set {
            if (_searchText == value)
                return;

            _searchText = value;
            FilterItems();
            SearchTextChanged.InvokeAsync(_searchText);
        }
    }

    protected override void OnParametersSet()
    {
        var incoming = new HashSet<string>(SelectedValues ?? []);
        if (!_selectedValues.SetEquals(incoming))
            _selectedValues = incoming;

        FilterItems();
    }

    private void FilterItems()
    {
        if (SearchTextChanged.HasDelegate) {
            _filteredItems = Items.OrderByDescending(item => item.Count).ToList();
            return;
        }

        var searchLower = _searchText.ToLowerInvariant();
        _filteredItems = Items.Where(item => FormatValue(item.Value).ToLowerInvariant().Contains(searchLower)).OrderByDescending(item => item.Count).ToList();
    }

    private async Task ToggleSelection(string value)
    {
        if (!_selectedValues.Add(value))
            _selectedValues.Remove(value);

        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync(_selectedValues.ToList());
    }

    private async Task OnRowKeyDown(KeyboardEventArgs e, string value)
    {
        if (e.Key is "Enter" or " ")
            await ToggleSelection(value);
    }

    private async Task SelectAll()
    {
        _selectedValues = _filteredItems.Where(item => item.Value != null)
            .Select(item => item.Value!.ToString() ?? string.Empty)
            .Where(value => !string.IsNullOrEmpty(value))
            .ToHashSet();

        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync(_selectedValues.ToList());
    }

    private async Task ClearAll()
    {
        _selectedValues.Clear();
        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync([]);
    }

    private string FormatValue(object? value)
    {
        if (value == null)
            return "(null)";

        if (ValueFormatter != null && value is T typedValue)
            return ValueFormatter(typedValue);

        return value.ToString() ?? "(empty)";
    }
}