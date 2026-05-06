using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Plan;

/// <summary>
/// Live bindings available while a plan runs — used by <see cref="AutomationPlanInterpolation.ExpandAsync" /> so templates can reference string vars, string lists, element
/// text/attributes, and the current page without waiting until <see cref="AutomationPlanRunResult" />.
/// </summary>
public sealed class AutomationPlanInterpolationBindings
{
    //todo make it more generic not just strings, stringlists, etc
    public IReadOnlyDictionary<string, string> Strings { get; init; } = null!;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists { get; init; } = null!;

    public IReadOnlyDictionary<string, IWebAutomationElement> Elements { get; init; } = null!;

    public IReadOnlyDictionary<string, object?> ContextItems { get; init; } = null!;

    /// <summary>When set, resolves <c>page.url</c> and <c>page.title</c>.</summary>
    public IWebAutomationBrowser? Browser { get; init; }
}