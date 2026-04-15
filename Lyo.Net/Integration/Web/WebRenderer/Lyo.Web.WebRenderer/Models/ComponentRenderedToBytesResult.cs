using System.Diagnostics;

namespace Lyo.Web.WebRenderer.Models;

/// <summary>Event arguments for component rendered to bytes events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ComponentRenderedToBytesResult(Type ComponentType, byte[] HtmlBytes, string Html, IReadOnlyDictionary<string, object>? ParameterDictionary, object? ComponentOptions)
{
    public override string ToString()
        => $"ComponentType: {ComponentType.FullName}, HtmlBytesLength: {HtmlBytes.Length}, HtmlLength: {Html.Length}, ParameterCount: {ParameterDictionary?.Count ?? 0}, HasComponentOptions: {ComponentOptions != null}";
}