using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// A titled block within a page: outlined surface, header row with its own actions, and a padded body. Use it to break a page into named areas instead of nesting
/// <c>MudCard</c> and <c>MudCardHeader</c> by hand, which is where the current pages disagree on padding and title size.
/// </summary>
/// <remarks>
/// Set <see cref="Flush" /> when the body is a table or grid that supplies its own padding, otherwise the block gains a double margin. Omit
/// <see cref="Title" /> and the header disappears, leaving a plain padded surface.
/// <code>
/// &lt;LyoSection Title="Parameters" Description="Values used on the next run"&gt;
///     &lt;Actions&gt;&lt;MudButton Size="Size.Small" OnClick="AddAsync"&gt;Add&lt;/MudButton&gt;&lt;/Actions&gt;
///     &lt;ChildContent&gt;&lt;LyoParameterEditor Rows="@_rows"/&gt;&lt;/ChildContent&gt;
/// &lt;/LyoSection&gt;
/// </code>
/// </remarks>
public partial class LyoSection
{
    /// <summary>Block heading. Leave blank to render a surface with no header.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Short line under the heading.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Material icon placed before the heading.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Typography used for the heading.</summary>
    [Parameter]
    public Typo TitleTypo { get; set; } = Typo.subtitle1;

    /// <summary>Markup inline with the heading, such as a count or status chip.</summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>Buttons for this block only, right-aligned in the header row.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Body of the block.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Removes body padding, for a grid or table that already pads its own cells.</summary>
    [Parameter]
    public bool Flush { get; set; }

    /// <summary>Draws a border. On by default; turn it off to sit the block inside another surface.</summary>
    [Parameter]
    public bool Outlined { get; set; } = true;

    /// <summary>MudBlazor elevation. Stays flat by default so nested blocks do not stack shadows.</summary>
    [Parameter]
    public int Elevation { get; set; }

    /// <summary>CSS class forwarded onto the surface.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the surface.</summary>
    [Parameter]
    public string? Style { get; set; }

    private bool HasHeader => !string.IsNullOrWhiteSpace(Title) || TitleContent is not null || Actions is not null;

    private string BodyCssClass => Flush ? "lyo-section-body-flush" : "lyo-section-body";
}
