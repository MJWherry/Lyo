using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Models;

/// <summary>What to do with a resolved element (JSON polymorphic on <c>type</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ClickElementAction), "click")]
[JsonDerivedType(typeof(InputTextElementAction), "inputText")]
[JsonDerivedType(typeof(SendKeysElementAction), "sendKeys")]
[JsonDerivedType(typeof(ClearElementAction), "clear")]
[JsonDerivedType(typeof(SubmitElementAction), "submit")]
[JsonDerivedType(typeof(SelectByTextElementAction), "selectByText")]
[JsonDerivedType(typeof(SelectByValueElementAction), "selectByValue")]
[JsonDerivedType(typeof(SelectByIndexElementAction), "selectByIndex")]
[JsonDerivedType(typeof(DropdownElementAction), "dropdown")]
public abstract record ElementAction;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ClickElementAction(bool ScrollIntoView = true) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"click (scrollIntoView: {ScrollIntoView})";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record InputTextElementAction(string Text, bool ClearFirst = true) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"inputText (clearFirst: {ClearFirst}, text: {AutomationDisplayText.Ellipsis(Text, 48)})";
}

/// <summary>Sends keys without clearing first (use for special key sequences).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SendKeysElementAction(string Keys) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"sendKeys ({AutomationDisplayText.Ellipsis(Keys, 64)})";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ClearElementAction : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => "clear";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record SubmitElementAction : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => "submit";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record SelectByTextElementAction(string Text) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"selectByText ({AutomationDisplayText.Ellipsis(Text, 64)})";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record SelectByValueElementAction(string Value) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"selectByValue ({AutomationDisplayText.Ellipsis(Value, 64)})";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record SelectByIndexElementAction(int Index) : ElementAction
{
    /// <inheritdoc />
    public override string ToString() => $"selectByIndex ({Index})";
}

/// <summary>
/// Generic dropdown: native <c>&lt;select&gt;</c> or custom menu. Default <see cref="DropdownElementAction.Mode" /> is <see cref="DropdownSelectionMode.Auto" /> (uses tag name).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DropdownElementAction(
    DropdownSelectionMode Mode = DropdownSelectionMode.Auto,
    string? SelectByText = null,
    string? SelectByValue = null,
    int? SelectByIndex = null,
    ElementLocator? OptionLocator = null,
    bool ClickTriggerFirst = true,
    string? ScopeParentRef = null) : ElementAction
{
    /// <inheritdoc />
    public override string ToString()
        => $"dropdown (mode: {Mode})";
}