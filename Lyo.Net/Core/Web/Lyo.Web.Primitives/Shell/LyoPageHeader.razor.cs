using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Standard page heading: breadcrumb trail, icon, title, description, a status chip row, and a right-aligned action area that wraps under the title on narrow
/// viewports. Use it at the top of every page and workbench so headings line up instead of each page inventing its own arrangement.
/// </summary>
/// <remarks>
/// <see cref="TitleContent" /> sits inline beside the title for something that belongs to the name itself, such as an environment badge. Status chips belong in
/// <see cref="Chips" />, which is a wrap row below the bar, for the same reason <c>LyoDialog</c> keeps its chips out of the header row: they collide with the actions.
/// <code>
/// &lt;LyoPageHeader Title="Nightly export" Description="Runs at 02:00 UTC" Icon="@Icons.Material.Filled.Schedule"
///                Breadcrumbs="@([new LyoBreadcrumb("Jobs", "/jobs"), new LyoBreadcrumb("Nightly export")])"&gt;
///     &lt;Chips&gt;&lt;LyoStatusChip Status="@run.Result" Palette="job"/&gt;&lt;/Chips&gt;
///     &lt;Actions&gt;&lt;MudButton OnClick="RunAsync"&gt;Run now&lt;/MudButton&gt;&lt;/Actions&gt;
/// &lt;/LyoPageHeader&gt;
/// </code>
/// </remarks>
public partial class LyoPageHeader
{
    /// <summary>Title of the page.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>One line under the title saying what the page is for or when its data was produced.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Material icon placed before the title.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Icon color, for pages that carry a domain accent.</summary>
    [Parameter]
    public Color IconColor { get; set; } = Color.Primary;

    /// <summary>Title typography. Drop to <c>h6</c> for a panel heading inside a page that already has an <c>h5</c>.</summary>
    [Parameter]
    public Typo TitleTypo { get; set; } = Typo.h5;

    /// <summary>Breadcrumb trail. Leave empty on a top-level page and nothing is rendered.</summary>
    [Parameter]
    public IReadOnlyList<LyoBreadcrumb>? Breadcrumbs { get; set; }

    /// <summary>Markup inline with the title, for a badge that reads as part of the name.</summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>Status chip row below the header bar. Keep chips here rather than in <see cref="TitleContent" />.</summary>
    [Parameter]
    public RenderFragment? Chips { get; set; }

    /// <summary>Buttons and menus for the page, right-aligned and wrapping under the title when space runs out.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Rules a line under the header. Off by default, because a page whose body starts with a card does not need one.</summary>
    [Parameter]
    public bool Divider { get; set; }

    /// <summary>CSS class forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }
}
