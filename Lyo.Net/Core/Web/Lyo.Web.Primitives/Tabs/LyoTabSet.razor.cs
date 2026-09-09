using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Primitives;

/// <summary>
/// <c>MudTabs</c> wrapper that restores the active tab from the query string, then from <see cref="ILyoUiPreferences" />, then from <see cref="DefaultTab" />.
/// Reloading or sharing the URL therefore lands on the same panel the 35 existing <c>MudTabs</c> sites currently drop.
/// </summary>
/// <remarks>
/// Put <see cref="LyoTab" /> children inside, not raw <c>MudTabPanel</c>, so each panel has a stable id. Query-string updates use <c>replace: true</c> so tab clicks
/// do not flood the browser history. Persistence is optional: omit <see cref="PersistKey" /> and only the URL is used; omit a registered
/// <see cref="ILyoUiPreferences" /> and the set still functions.
/// <code>
/// &lt;LyoTabSet PersistKey="job-detail" DefaultTab="logs"&gt;
///     &lt;LyoTab Id="overview" Text="Overview"&gt;...&lt;/LyoTab&gt;
///     &lt;LyoTab Id="logs" Text="Logs"&gt;...&lt;/LyoTab&gt;
/// &lt;/LyoTabSet&gt;
/// </code>
/// </remarks>
public partial class LyoTabSet : IDisposable
{
    private const string PreferencePrefix = "pref_tab_";
    private readonly List<LyoTab> _tabs = [];
    private bool _applied;
    private bool _syncing;

    /// <summary>Tab panels. Use <see cref="LyoTab" /> so each panel can be addressed by id.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Query-string key that holds the active tab id. Defaults to <c>tab</c>. Set to null or blank to keep the URL clean, such as inside a dialog.
    /// </summary>
    [Parameter]
    public string? QueryParameter { get; set; } = "tab";

    /// <summary>
    /// Local-storage key suffix. The stored key is <c>pref_tab_{PersistKey}</c>. Leave unset to skip persistence and rely on the URL or the default tab.
    /// </summary>
    [Parameter]
    public string? PersistKey { get; set; }

    /// <summary>Tab id opened when neither the URL nor storage has a match.</summary>
    [Parameter]
    public string? DefaultTab { get; set; }

    /// <summary>MudBlazor elevation applied to the tab header.</summary>
    [Parameter]
    public int Elevation { get; set; }

    /// <summary>Draws a border around the header.</summary>
    [Parameter]
    public bool Outlined { get; set; }

    /// <summary>Rounds the corners of the header.</summary>
    [Parameter]
    public bool Rounded { get; set; }

    /// <summary>Draws a line beneath the header.</summary>
    [Parameter]
    public bool Border { get; set; }

    /// <summary>Centers the tab headers.</summary>
    [Parameter]
    public bool Centered { get; set; }

    /// <summary>Applies panel transitions to the container. On by default so panel padding does not jump around.</summary>
    [Parameter]
    public bool ApplyEffectsToContainer { get; set; } = true;

    /// <summary>Keeps inactive panels in the DOM. Turn on when a panel holds an editor whose state would be costly to rebuild.</summary>
    [Parameter]
    public bool KeepPanelsAlive { get; set; }

    /// <summary>CSS class applied to each panel body.</summary>
    [Parameter]
    public string? PanelClass { get; set; } = "pt-3";

    /// <summary>CSS class applied to the tabs control.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style applied to the tabs control.</summary>
    [Parameter]
    public string? Style { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private int ActiveIndex { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

    /// <inheritdoc />
    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;

    internal void Register(LyoTab tab)
    {
        if (_tabs.Contains(tab))
            return;

        _tabs.Add(tab);
        _ = ApplyInitialAsync();
    }

    internal void Unregister(LyoTab tab) => _tabs.Remove(tab);

    /// <summary>Lowercases <paramref name="text" /> and replaces runs of non-alphanumeric characters with one hyphen.</summary>
    public static string Slug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new System.Text.StringBuilder(text.Length);
        var pending = false;
        foreach (var character in text.Trim()) {
            if (char.IsLetterOrDigit(character)) {
                if (pending && builder.Length > 0)
                    builder.Append('-');

                pending = false;
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            pending = builder.Length > 0;
        }

        return builder.ToString();
    }

    private async Task OnActiveIndexChangedAsync(int index)
    {
        if (_syncing || index == ActiveIndex)
            return;

        ActiveIndex = index;
        var id = IdAt(index);
        if (string.IsNullOrWhiteSpace(id))
            return;

        WriteQuery(id);
        await WritePreferenceAsync(id);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (_syncing || _tabs.Count == 0)
            return;

        var fromQuery = ReadQuery();
        if (string.IsNullOrWhiteSpace(fromQuery))
            return;

        var index = IndexOf(fromQuery);
        if (index < 0 || index == ActiveIndex)
            return;

        ActiveIndex = index;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task ApplyInitialAsync()
    {
        if (_applied || _tabs.Count == 0)
            return;

        _applied = true;
        var fromQuery = ReadQuery();
        var fromStore = await ReadPreferenceAsync();
        var chosen = FirstMatch(fromQuery, fromStore, DefaultTab, _tabs[0].ResolvedId);
        var index = IndexOf(chosen);
        if (index < 0)
            index = 0;

        _syncing = true;
        try {
            ActiveIndex = index;
            var id = IdAt(index);
            if (!string.IsNullOrWhiteSpace(id) && !string.Equals(fromQuery, id, StringComparison.OrdinalIgnoreCase))
                WriteQuery(id);
        }
        finally {
            _syncing = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private string? ReadQuery()
    {
        if (string.IsNullOrWhiteSpace(QueryParameter))
            return null;

        var query = Navigation.ToAbsoluteUri(Navigation.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0 || !string.Equals(Uri.UnescapeDataString(parts[0]), QueryParameter, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private void WriteQuery(string id)
    {
        if (string.IsNullOrWhiteSpace(QueryParameter))
            return;

        var next = Navigation.GetUriWithQueryParameter(QueryParameter, id);
        if (string.Equals(next, Navigation.Uri, StringComparison.Ordinal))
            return;

        _syncing = true;
        try {
            Navigation.NavigateTo(next, new NavigationOptions { ReplaceHistoryEntry = true });
        }
        finally {
            _syncing = false;
        }
    }

    private async Task<string?> ReadPreferenceAsync()
    {
        if (string.IsNullOrWhiteSpace(PersistKey) || Services.GetService<ILyoUiPreferences>() is not { } preferences)
            return null;

        try {
            return await preferences.GetPreferenceAsync(PreferenceKey);
        }
        catch (Exception) {
            return null;
        }
    }

    private async Task WritePreferenceAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(PersistKey) || Services.GetService<ILyoUiPreferences>() is not { } preferences)
            return;

        try {
            await preferences.SetPreferenceAsync(PreferenceKey, id);
        }
        catch (Exception) {
            // Local storage is unavailable during prerender or when the browser refuses it.
        }
    }

    private string PreferenceKey => $"{PreferencePrefix}{PersistKey}";

    private int IndexOf(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return -1;

        for (var i = 0; i < _tabs.Count; i++) {
            if (string.Equals(_tabs[i].ResolvedId, id, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private string? IdAt(int index) => index >= 0 && index < _tabs.Count ? _tabs[index].ResolvedId : null;

    private static string? FirstMatch(params string?[] candidates)
    {
        foreach (var candidate in candidates) {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return null;
    }
}
