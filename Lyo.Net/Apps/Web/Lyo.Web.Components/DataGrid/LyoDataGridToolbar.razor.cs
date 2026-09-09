using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Shared action strip for <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" />. Owns the search field so post-load refocus stays on this
/// component; the grid calls <see cref="FocusSearchAsync" /> from its query <c>finally</c>.
/// </summary>
public partial class LyoDataGridToolbar
{
    private MudTextField<string>? _searchField;

    /// <summary>Typed or projected grid that supplies chrome state and actions.</summary>
    [Parameter]
    [EditorRequired]
    public required ILyoDataGridToolbarHost Host { get; set; }

    /// <summary>True when the search field had focus at the last focus/blur event.</summary>
    public bool SearchHadFocus { get; private set; }

    /// <summary>Focuses the quick-search field after a reload that should keep the caret in place.</summary>
    public async Task FocusSearchAsync()
    {
        if (_searchField is not null)
            await _searchField.FocusAsync();
    }

    private bool ShowSearchOrFilters
        => Host.HasFeature(LyoDataGridFeatureFlags.Searchable)
           || (Host.HasFeature(LyoDataGridFeatureFlags.Filterable) && Host.FilterPropertyDefinitions.Count > 0);

    private string? SearchText
    {
        get => Host.SearchText;
        set => Host.SearchText = value;
    }

    private Task OnSearchDebouncedAsync(string value) => Host.OnSearchDebouncedAsync(value);

    private Task OnSearchKeyDownAsync(KeyboardEventArgs e) => Host.OnSearchKeyDownAsync(e);
}
