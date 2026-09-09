using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Form;

public partial class LyoFormGrid
{
    /// <summary>MudGrid spacing between fields. Default is a compact 2.</summary>
    [Parameter]
    public int Spacing { get; set; } = 2;

    /// <summary>
    /// Form fields. <see cref="LyoFormInput{TModel,TValue}" /> children detect this grid via a cascading value and wrap themselves in a <c>MudItem</c> sized from their
    /// <c>FieldSize</c> (or derived from the property type), so callers only declare fields.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
