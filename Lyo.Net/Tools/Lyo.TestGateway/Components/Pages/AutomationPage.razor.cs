using Lyo.Api.Client;
using Lyo.Web.Automation.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages;

public partial class AutomationPage
{
    [Inject]
    private IAutomationPlanClient Client { get; set; } = null!;

    [Inject]
    private IApiClient ApiClient { get; set; } = null!;

    protected override string PageName { get; set; } = "Automation plan";
}
