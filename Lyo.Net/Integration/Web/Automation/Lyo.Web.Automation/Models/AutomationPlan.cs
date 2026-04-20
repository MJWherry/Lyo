using System.Diagnostics;

namespace Lyo.Web.Automation.Models;

/// <summary>Automation definition: ordered steps (immutable list) and optional display name.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AutomationPlan(string? Name, IReadOnlyList<AutomationStepDefinition> Steps)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var label = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : AutomationDisplayText.Ellipsis(Name!, 80);
        return $"AutomationPlan \"{label}\": {Steps.Count} step(s)";
    }
}
