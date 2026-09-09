using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Drawer links built from <see cref="LyoWorkbenchRegistry" />, grouped by <see cref="ILyoWorkbenchDescriptor.Category" />. Optional <see cref="ChildContent" />
/// is rendered first so a host can keep hand-rolled links above the registered workbenches.
/// </summary>
public partial class LyoNavMenu
{
    /// <summary>Links that are not in the registry, such as Home and auth pages.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Categories to skip when the host already lists those packages by hand.</summary>
    [Parameter]
    public IReadOnlyList<string>? ExcludeCategories { get; set; }

    [Inject]
    private LyoWorkbenchRegistry Registry { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private IEnumerable<IGrouping<string, ILyoWorkbenchDescriptor>> Groups
        => Registry.All
            .Where(item => ExcludeCategories is null || !ExcludeCategories.Contains(item.Category, StringComparer.OrdinalIgnoreCase))
            .GroupBy(item => string.IsNullOrWhiteSpace(item.Category) ? "Other" : item.Category);

    private bool IsExpanded(IGrouping<string, ILyoWorkbenchDescriptor> group)
    {
        var current = Navigation.ToBaseRelativePath(Navigation.Uri).Trim('/');
        return group.Any(item => string.Equals(current, item.Route.Trim('/'), StringComparison.OrdinalIgnoreCase)
            || current.StartsWith(item.Route.Trim('/') + "/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(current, $"workbench/{item.Slug}", StringComparison.OrdinalIgnoreCase));
    }
}
