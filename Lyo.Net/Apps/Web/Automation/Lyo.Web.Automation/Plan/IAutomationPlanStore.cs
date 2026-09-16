using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

/// <summary>Named automation plans. The in-memory registry and the Postgres store both implement this.</summary>
public interface IAutomationPlanStore
{
    /// <summary>Stored plans, in name order.</summary>
    IReadOnlyList<AutomationPlanSummary> List();

    /// <summary>The plan with this name, or null.</summary>
    AutomationPlan? GetByName(string name);

    /// <summary>The plan with this id, or null.</summary>
    AutomationPlan? GetById(Guid id);

    /// <summary>Stores an empty plan. A name that already exists is returned unchanged.</summary>
    AutomationPlan Create(string name);

    /// <summary>Replaces a stored plan. Returns false when <paramref name="name" /> is not stored.</summary>
    bool TryReplace(string name, AutomationPlan plan);
}
