using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// One panel inside <see cref="LyoTabSet" />. Registers its <see cref="Id" /> with the parent so the set can deep-link and persist the active tab by name rather than
/// by index, which would break when a panel is added or reordered.
/// </summary>
public partial class LyoTab : IDisposable
{
    /// <summary>Stable id written to the query string and local storage. Defaults to a slug of <see cref="Text" />.</summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>Label on the tab.</summary>
    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional Material icon on the tab header.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Disables the tab without hiding it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Tooltip on the tab header.</summary>
    [Parameter]
    public string? ToolTip { get; set; }

    /// <summary>Body of the panel.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [CascadingParameter]
    private LyoTabSet? Owner { get; set; }

    /// <summary>Id the parent uses for deep-links. Never empty once the tab has a label.</summary>
    public string ResolvedId => string.IsNullOrWhiteSpace(Id) ? LyoTabSet.Slug(Text) : Id.Trim();

    /// <inheritdoc />
    protected override void OnInitialized() => Owner?.Register(this);

    /// <inheritdoc />
    public void Dispose() => Owner?.Unregister(this);
}
