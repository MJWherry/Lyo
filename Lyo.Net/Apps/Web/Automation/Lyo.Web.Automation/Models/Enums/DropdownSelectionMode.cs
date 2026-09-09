using System.Text.Json.Serialization;

namespace Lyo.Web.Automation.Models.Enums;

/// <summary>Chooses how a <see cref="ElementAction" /> <c>dropdown</c> step applies to its target element.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DropdownSelectionMode>))]
public enum DropdownSelectionMode
{
    /// <summary>Detect <c>select</c> versus custom by tag name; use native selection fields or custom <c>optionLocator</c> as appropriate.</summary>
    Auto,

    /// <summary>Call <see cref="Abstractions.IWebAutomationElement.SelectByTextAsync" /> / <c>Value</c> / <c>Index</c> on the target (it must be a <c>select</c>).</summary>
    Native,

    /// <summary>Open the menu (when <c>clickTriggerFirst</c>) then resolve and click an option through <c>optionLocator</c>.</summary>
    Custom
}