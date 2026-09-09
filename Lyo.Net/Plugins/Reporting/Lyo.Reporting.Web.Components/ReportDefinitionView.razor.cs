using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Web.Components;

public partial class ReportDefinitionView
{
    [Parameter]
    [EditorRequired]
    public ReportDefinitionRes Definition { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "Reporting/Definition";

    /// <summary>When set, shows a Design action that opens the composition workbench for this definition.</summary>
    [Parameter]
    public EventCallback<Guid> Design { get; set; }

    private Task OnDesign() => Design.InvokeAsync(Definition.Id);
}
