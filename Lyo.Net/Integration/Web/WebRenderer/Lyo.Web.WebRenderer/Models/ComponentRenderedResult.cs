using System.Diagnostics;

namespace Lyo.Web.WebRenderer.Models;

/// <summary>Event arguments for component rendered events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ComponentRenderedResult(Type ComponentType, string Html, Dictionary<string, object>? ParameterDictionary, object? ComponentOptions)
{
    public override string ToString()
        => $"ComponentType: {ComponentType.FullName}, HtmlLength: {Html.Length}, ParameterCount: {ParameterDictionary?.Count ?? 0}, HasComponentOptions: {ComponentOptions != null}";
}