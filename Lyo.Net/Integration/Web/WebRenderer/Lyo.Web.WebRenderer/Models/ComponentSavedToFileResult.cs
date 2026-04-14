using System.Diagnostics;

namespace Lyo.Web.WebRenderer.Models;

/// <summary>Event arguments for component saved to file events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ComponentSavedToFileResult(Type ComponentType, string FilePath, string Html, Dictionary<string, object>? ParameterDictionary, object? ComponentOptions)
{
    public override string ToString()
        => $"ComponentType: {ComponentType.FullName}, FilePath: {FilePath}, HtmlLength: {Html.Length}, ParameterCount: {ParameterDictionary?.Count ?? 0}, HasComponentOptions: {ComponentOptions != null}";
}