using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Host;

/// <summary>
/// Shared host chrome: theme provider, drawer, app bar, dark-mode toggle stored in <see cref="ClientStore" />, and the Blazor error UI. Auth and domain nav stay
/// in the host via <see cref="DrawerHeader" />, <see cref="DrawerNav" />, and <see cref="AppBarEnd" />.
/// </summary>
/// <remarks>
/// When <see cref="DrawerNav" /> is omitted the drawer renders <see cref="LyoNavMenu" /> from the workbench registry. Add the client services with
/// <c>AddLyoWebShell</c> before mounting this component.
/// </remarks>
public partial class LyoAppShell
{
    private bool _drawerOpen;
    private bool _isDarkMode;
    private bool _initialized;

    /// <summary>App-bar text, usually the current page name.</summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>Theme. Falls back to <see cref="LyoThemes.Default" />.</summary>
    [Parameter]
    public MudTheme? Theme { get; set; }

    /// <summary>Shows the dark-mode switch. Defaults to on.</summary>
    [Parameter]
    public bool ShowThemeToggle { get; set; } = true;

    /// <summary>Top of the drawer, usually an avatar and sign-in state.</summary>
    [Parameter]
    public RenderFragment? DrawerHeader { get; set; }

    /// <summary>Drawer links. Leave out to use the workbench registry menu.</summary>
    [Parameter]
    public RenderFragment? DrawerNav { get; set; }

    /// <summary>Trailing app-bar content, usually the user menu.</summary>
    [Parameter]
    public RenderFragment? AppBarEnd { get; set; }

    /// <summary>Page body content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
            return;

        _isDarkMode = await ClientStore.GetDarkThemeAsync();
        _initialized = true;
        StateHasChanged();
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task OnDarkModeChanged(bool value)
    {
        _isDarkMode = value;
        await ClientStore.SetDarkThemeAsync(value);
    }
}
