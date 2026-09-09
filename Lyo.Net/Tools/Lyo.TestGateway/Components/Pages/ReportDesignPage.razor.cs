using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages;

public partial class ReportDesignPage
{
    [Parameter]
    public Guid? DefinitionId { get; set; }

    protected override string PageName { get; set; } = "Report design";
}
