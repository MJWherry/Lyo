using System.Text.Json;

namespace Lyo.Algorithms.Models.Request;

/// <summary>Body for <c>POST Algorithms/preview</c>. The revision is compiled and run, then discarded.</summary>
public sealed class AlgorithmPreviewReq
{
    /// <summary>Step name. A catalog key. Empty uses <c>preview</c>.</summary>
    public string? Key { get; set; }

    /// <summary>Catalog kind stored on this draft.</summary>
    public AlgorithmKind Kind { get; set; }

    /// <summary>Expression source, or the startup key when <see cref="Kind" /> is <see cref="AlgorithmKind.Named" />.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Scorer JSON, or the type binding (<c>result</c>, <c>parameters</c>, <c>calls</c>, <c>inputType</c>) for an expression.</summary>
    public string? Payload { get; set; }

    /// <summary>Identifier values. Each value is converted to that name's parameter type.</summary>
    public JsonElement Context { get; set; }
}
