using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Form;

public partial class LyoExpandableCard
{
    /// <summary>Optional HTML <c>id</c> override for the root node.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>Primary row fields. Stays visible when the card is collapsed.</summary>
    [Parameter]
    public RenderFragment? Summary { get; set; }

    /// <summary>Extra fields displayed after the trailing expand arrow is opened.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _expanded;

    private void Toggle() => _expanded = !_expanded;
}
