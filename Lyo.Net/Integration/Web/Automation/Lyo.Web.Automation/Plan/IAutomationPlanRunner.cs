using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Plan;

/// <summary>Executes serializable automation plans using a browser session.</summary>
public interface IAutomationPlanRunner
{
    Task RunAsync(IWebAutomationSession session, AutomationPlan plan, ILogger? logger, CancellationToken ct);

    Task<AutomationPlanRunResult> RunWithResultAsync(
        IWebAutomationSession session,
        AutomationPlan plan,
        AutomationPlanRuntimeOptions? runtime,
        ILogger? logger,
        CancellationToken ct);
}