using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.CheckSelect;

/// <summary>
/// A generic multi-select dropdown with a checkbox list and optional search — modelled on the filter-panel pattern. Renders as an outlined trigger button that opens a
/// <c>MudMenu</c> popover.
/// </summary>
/// <typeparam name="TValue">Value type for each option (e.g. an enum).</typeparam>
public partial class LyoCheckSelect<TValue>
    where TValue : notnull
{
    private List<LyoSelectOption<TValue>> _filteredItems = [];
    private MudMenu? _menu;
    private string? _search = string.Empty;
    private HashSet<TValue> _selected = new(EqualityComparer<TValue>.Default);

    /// <summary>Label shown above the trigger button.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Placeholder text shown in the trigger when nothing is selected.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "Select...";

    /// <summary>The full list of options to show in the dropdown.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<LyoSelectOption<TValue>> Items { get; set; } = [];

    /// <summary>Currently selected values.</summary>
    [Parameter]
    public IEnumerable<TValue> SelectedValues { get; set; } = [];

    /// <summary>Fires whenever the selection changes.</summary>
    [Parameter]
    public EventCallback<IEnumerable<TValue>> SelectedValuesChanged { get; set; }

    /// <summary>When true, shows a search field inside the dropdown. Default: false.</summary>
    [Parameter]
    public bool Searchable { get; set; }

    private string TriggerText {
        get {
            if (_selected.Count == 0)
                return Placeholder;

            if (_selected.Count == 1) {
                var single = _selected.First();
                var match = Items.FirstOrDefault(i => EqualityComparer<TValue>.Default.Equals(i.Value, single));
                return match?.Label ?? single.ToString() ?? Placeholder;
            }

            return $"{_selected.Count} selected";
        }
    }

    private bool IsPlaceholder => _selected.Count == 0;

    private string? Search {
        get => _search;
        set {
            if (_search == value)
                return;

            _search = value;
            ApplyFilter();
        }
    }

    protected override void OnParametersSet()
    {
        var incoming = new HashSet<TValue>(SelectedValues, EqualityComparer<TValue>.Default);
        if (!_selected.SetEquals(incoming))
            _selected = incoming;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!Searchable || string.IsNullOrWhiteSpace(_search)) {
            _filteredItems = Items.ToList();
            return;
        }

        var lower = _search.ToLowerInvariant();
        _filteredItems = Items.Where(i => i.Label.ToLowerInvariant().Contains(lower)).ToList();
    }

    private async Task Toggle(TValue value)
    {
        if (!_selected.Add(value))
            _selected.Remove(value);

        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync(_selected.ToList());
    }

    private async Task OnItemKeyDown(KeyboardEventArgs e, TValue value)
    {
        if (e.Key is "Enter" or " ")
            await Toggle(value);
    }

    private async Task SelectAll()
    {
        _selected = _filteredItems.Select(i => i.Value).ToHashSet(EqualityComparer<TValue>.Default);
        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync(_selected.ToList());
    }

    private async Task ClearAll()
    {
        _selected.Clear();
        await InvokeAsync(StateHasChanged);
        await SelectedValuesChanged.InvokeAsync([]);
    }

    private Task CloseAsync() => _menu?.CloseMenuAsync() ?? Task.CompletedTask;
}