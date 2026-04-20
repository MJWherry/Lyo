using System.Diagnostics;

namespace Lyo.Web.Automation.Models;

/// <summary>Serializable locator (resolved by each engine: Selenium <c>By</c>, Playwright selector, etc.).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed partial record ElementLocator(ElementLocatorKind Kind, string Value)
{
    /// <inheritdoc />
    public override string ToString()
        => $"{Kind}: {AutomationDisplayText.Ellipsis(Value)}";
}
