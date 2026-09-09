using Lyo.Reporting.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lyo.Web.WebRenderer.Tests;

/// <summary>Test-only host so painter methods can emit HTML without <see cref="Lyo.Reporting.Web.Components.ReportViewer{T}" />.</summary>
public sealed class ReportPaintProbe : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public ReportViewPainter Paint { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public Action<RenderTreeBuilder, ReportViewPainter> Draw { get; set; } = null!;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Draw(builder, Paint);
}
