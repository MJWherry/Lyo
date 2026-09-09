namespace Lyo.Web.Primitives;

/// <summary>
/// Describes one embeddable workbench so a host can list it in <see cref="LyoNavMenu" /> and render it from <see cref="LyoWorkbenchHost" /> without a dedicated page.
/// </summary>
public interface ILyoWorkbenchDescriptor
{
    /// <summary>Title used in nav and on the page.</summary>
    string Title { get; }

    /// <summary>Material icon for the nav item.</summary>
    string Icon { get; }

    /// <summary>Drawer group, for example <c>Infrastructure</c>. Items that share a category collapse together.</summary>
    string Category { get; }

    /// <summary>Href used by the nav menu. Existing host pages keep their current route; new packages can use <c>workbench/{slug}</c>.</summary>
    string Route { get; }

    /// <summary>Component type rendered by <see cref="LyoWorkbenchHost" />.</summary>
    Type ComponentType { get; }

    /// <summary>Slug matched by <see cref="LyoWorkbenchHost" />. Defaults to the last segment of <see cref="Route" />.</summary>
    string Slug { get; }
}
