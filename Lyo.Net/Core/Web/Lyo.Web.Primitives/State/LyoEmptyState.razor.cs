using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// The "nothing here" panel: centred icon, headline, explanation, and optional actions. Use it wherever a list, grid, or panel can legitimately have no rows, instead
/// of leaving the area blank or reaching for an informational <c>MudAlert</c>, which reads as a problem rather than an empty set.
/// </summary>
/// <remarks>
/// <see cref="LyoResultBoundary{T}" /> renders one of these by default, so most callers get it without composing it directly.
/// <code>
/// &lt;LyoEmptyState Title="No schedules yet"
///                Description="Schedules you create will appear here."
///                Icon="@Icons.Material.Filled.EventBusy"&gt;
///     &lt;Actions&gt;&lt;MudButton OnClick="CreateAsync"&gt;New schedule&lt;/MudButton&gt;&lt;/Actions&gt;
/// &lt;/LyoEmptyState&gt;
/// </code>
/// </remarks>
public partial class LyoEmptyState
{
    /// <summary>Headline, stating what is missing rather than that something failed.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = "Nothing to show";

    /// <summary>Optional second line explaining how rows get here, or which filter is hiding them.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Material icon above the headline. Set to null or blank to drop it.</summary>
    [Parameter]
    public string? Icon { get; set; } = Icons.Material.Filled.Inbox;

    /// <summary>Icon size. Large suits a full page, medium a panel or dialog body.</summary>
    [Parameter]
    public Size IconSize { get; set; } = Size.Large;

    /// <summary>Headline typography. Drop to <c>subtitle1</c> inside a small panel.</summary>
    [Parameter]
    public Typo TitleTypo { get; set; } = Typo.h6;

    /// <summary>Buttons or links offering the next step, such as clearing a filter or creating the first row.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>CSS class forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }
}
