using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Extensions;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Query.Models.Parameters;

/// <summary>
/// Picker source for definition parameters. Serialized as JSON on the definition parameter <c>Options</c> column. Does not replace the scalar <c>Value</c> (default /
/// selected key).
/// </summary>
[DebuggerDisplay("{Kind}")]
public sealed class ParameterOptions
{
    /// <summary>Static list vs root query template.</summary>
    public ParameterOptionsKind Kind { get; set; }

    /// <summary>Required when <see cref="Kind" /> is <see cref="ParameterOptionsKind.Static" />.</summary>
    public List<ParameterOptionsItem> Items { get; set; } = [];

    /// <summary>Relative route for root query (default <c>Query</c>). Used when <see cref="Kind" /> is <see cref="ParameterOptionsKind.Query" />.</summary>
    public string? QueryRoute { get; set; }

    /// <summary>
    /// Root <see cref="QueryReq" /> template. Table is <see cref="QueryReq.From" />.<c>EntityType</c>. May contain <c>{{SiblingParamKey}}</c> placeholders in where values for
    /// live input binding.
    /// </summary>
    public QueryReq? Query { get; set; }

    /// <summary>Effective query route (defaults to <c>Query</c>).</summary>
    [JsonIgnore]
    public string EffectiveQueryRoute => QueryRoute.IsNullOrWhitespace() ? "Query" : QueryRoute!.Trim();

    public override string ToString() => Kind == ParameterOptionsKind.Static ? $"Static Items={Items.Count}" : $"Query Route={EffectiveQueryRoute} From={Query?.From.EntityType}";
}