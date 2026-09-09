using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lyo.Web.WebRenderer.Tests;

/// <summary>Minimal <see cref="IComponent" /> used to exercise Type and string render overloads.</summary>
public sealed class HelloRenderComponent : ComponentBase
{
    [Parameter]
    public string? Name { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Hello " + (Name ?? "world"));
        builder.CloseElement();
    }
}
