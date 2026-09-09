using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Plan;

/// <summary>
/// Live bindings available while a plan runs. Used by <see cref="AutomationPlanInterpolation.ExpandAsync" /> so templates can reference string vars, string lists, element
/// text/attributes, and the current page without waiting until <see cref="AutomationPlanRunResult" />.
/// </summary>
/// <remarks>
/// Bindings stay split by kind (<see cref="Strings" />, <see cref="StringLists" />, <see cref="Elements" />, <see cref="ContextItems" />) so placeholder resolution stays
/// type-safe. A single untyped bag would push conversion into every template expand.
/// </remarks>
public sealed class AutomationPlanInterpolationBindings
{
    /// <summary>Named string variables from earlier steps.</summary>
    public IReadOnlyDictionary<string, string> Strings { get; init; } = null!;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists { get; init; } = null!;

    public IReadOnlyDictionary<string, IWebAutomationElement> Elements { get; init; } = null!;

    public IReadOnlyDictionary<string, object?> ContextItems { get; init; } = null!;

    /// <summary>If set, resolves <c>page.url</c> and <c>page.title</c>.</summary>
    public IWebAutomationBrowser? Browser { get; init; }
}