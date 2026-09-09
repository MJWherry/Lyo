using Lyo.Parameters;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Extensions;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Query.Models.Parameters;

/// <summary>
/// Picker source for definition parameters. Stored as JSON on the definition parameter <c>Options</c> column. Does not replace the scalar <c>Value</c> (default /
/// selected key). Query and Sproc kinds can also materialize row JSON for report grids.
/// </summary>
[DebuggerDisplay("{Kind}")]
public sealed class ParameterOptions
{
    /// <summary>Static list versus a root query template or stored procedure.</summary>
    public ParameterOptionsKind Kind { get; set; }

    /// <summary>Needed when <see cref="Kind" /> is <see cref="ParameterOptionsKind.Static" />.</summary>
    public List<ParameterOptionsItem> Items { get; set; } = [];

    /// <summary>Relative route for the root query (starts as <c>Query</c>). Used when <see cref="Kind" /> is <see cref="ParameterOptionsKind.Query" />.</summary>
    public string? QueryRoute { get; set; }

    /// <summary>
    /// Root <see cref="QueryReq" /> template. Table is <see cref="QueryReq.From" />.<c>EntityType</c>. Where values may hold <c>{{SiblingParamKey}}</c> placeholders for
    /// live input binding.
    /// </summary>
    public QueryReq? Query { get; set; }

    /// <summary>PostgreSQL <c>schema.func</c> name when <see cref="Kind" /> is <see cref="ParameterOptionsKind.Sproc" />.</summary>
    public string? StoredProcName { get; set; }

    /// <summary>Stored-procedure argument name → literal or <c>{{SiblingParamKey}}</c>.</summary>
    public Dictionary<string, string> SprocParameters { get; set; } = [];

    /// <summary>Column used as the picker key for Sproc rows. Defaults to <c>Key</c>.</summary>
    public string? KeyField { get; set; }

    /// <summary>Column used as the picker label for Sproc rows. Defaults to <c>Value</c>.</summary>
    public string? LabelField { get; set; }

    /// <summary>Effective query route (starts as <c>Query</c>).</summary>
    [JsonIgnore]
    public string EffectiveQueryRoute => QueryRoute.IsNullOrWhitespace() ? "Query" : QueryRoute!.Trim();

    /// <summary>Effective picker key column for Sproc rows.</summary>
    [JsonIgnore]
    public string EffectiveKeyField => string.IsNullOrWhiteSpace(KeyField) ? "Key" : KeyField!.Trim();

    /// <summary>Effective picker label column for Sproc rows.</summary>
    [JsonIgnore]
    public string EffectiveLabelField => string.IsNullOrWhiteSpace(LabelField) ? "Value" : LabelField!.Trim();

    public override string ToString()
        => Kind switch {
            ParameterOptionsKind.Static => $"Static Items={Items.Count}",
            ParameterOptionsKind.Query => $"Query Route={EffectiveQueryRoute} From={Query?.From.EntityType}",
            ParameterOptionsKind.Sproc => $"Sproc {StoredProcName}",
            var _ => Kind.ToString()
        };
}
