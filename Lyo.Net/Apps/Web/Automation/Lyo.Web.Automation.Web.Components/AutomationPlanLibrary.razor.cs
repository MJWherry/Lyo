using Lyo.Web.Automation.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>Lists plans and asks the host to create or open one. It does not call HTTP.</summary>
public partial class AutomationPlanLibrary : ComponentBase
{
    private string _name = "";

    /// <summary>Plans the host already loaded.</summary>
    [Parameter]
    public IReadOnlyList<AutomationPlanSummary> Plans { get; set; } = [];

    /// <summary>Raised with the typed name when Create is clicked.</summary>
    [Parameter]
    public EventCallback<string> OnCreate { get; set; }

    /// <summary>Raised with the plan name when a row is clicked.</summary>
    [Parameter]
    public EventCallback<string> OnOpen { get; set; }

    private async Task Create()
    {
        var name = _name.Trim();
        if (name.Length == 0)
            return;

        await OnCreate.InvokeAsync(name);
        _name = "";
    }
}