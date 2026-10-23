using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Query.Web.Components;

/// <summary>Serializes <see cref="QueryWorkbenchRunMode" /> as <c>Query</c>, <c>QueryProject</c>, or <c>RootQuery</c>.</summary>
public sealed class QueryWorkbenchRunModeJsonConverter : JsonConverter<QueryWorkbenchRunMode>
{
    public override QueryWorkbenchRunMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unrecognized {nameof(QueryWorkbenchRunMode)} value.");

        var name = reader.GetString();
        if (string.Equals(name, "Query", StringComparison.OrdinalIgnoreCase))
            return QueryWorkbenchRunMode.Query;

        if (string.Equals(name, "QueryProject", StringComparison.OrdinalIgnoreCase))
            return QueryWorkbenchRunMode.QueryProject;

        if (string.Equals(name, "RootQuery", StringComparison.OrdinalIgnoreCase))
            return QueryWorkbenchRunMode.RootQuery;

        throw new JsonException($"Unrecognized {nameof(QueryWorkbenchRunMode)} value.");
    }

    public override void Write(Utf8JsonWriter writer, QueryWorkbenchRunMode value, JsonSerializerOptions options)
    {
        var name = value switch {
            QueryWorkbenchRunMode.QueryProject => nameof(QueryWorkbenchRunMode.QueryProject),
            QueryWorkbenchRunMode.RootQuery => nameof(QueryWorkbenchRunMode.RootQuery),
            var _ => nameof(QueryWorkbenchRunMode.Query)
        };

        writer.WriteStringValue(name);
    }
}