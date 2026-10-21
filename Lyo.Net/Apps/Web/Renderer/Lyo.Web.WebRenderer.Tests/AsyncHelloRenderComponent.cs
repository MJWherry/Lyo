using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lyo.Web.WebRenderer.Tests;

/// <summary>Yields during init so render continues off the calling thread.</summary>
public sealed class AsyncHelloRenderComponent : ComponentBase
{
    [Parameter]
    public string? Name { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
        Name = (Name ?? "world") + "!";
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Hello " + Name);
        builder.CloseElement();
    }
}