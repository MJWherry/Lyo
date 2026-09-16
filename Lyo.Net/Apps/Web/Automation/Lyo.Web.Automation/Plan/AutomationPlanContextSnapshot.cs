using System.Diagnostics;

namespace Lyo.Web.Automation.Plan;

/// <summary>JSON-safe view of plan variables after one step. Element handles stay as ref names.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanContextSnapshot
{
    /// <summary>String variables.</summary>
    public Dictionary<string, string> Strings { get; set; } = new(StringComparer.Ordinal);

    /// <summary>String-list variables.</summary>
    public Dictionary<string, List<string>> StringLists { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Custom context bag. Values are strings or JSON text.</summary>
    public Dictionary<string, string?> ContextItems { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Element ref names stored at this point.</summary>
    public List<string> ElementRefs { get; set; } = [];

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanContextSnapshot strings={Strings.Count}, lists={StringLists.Count}, context={ContextItems.Count}, elements={ElementRefs.Count}";
}
