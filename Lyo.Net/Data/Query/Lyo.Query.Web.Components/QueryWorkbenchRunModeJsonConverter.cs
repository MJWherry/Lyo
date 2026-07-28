using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Query.Web.Components;

/// <summary>Serializes <see cref="QueryWorkbenchRunMode" />; accepts legacy <c>QuerySelect</c> and numeric ordinals.</summary>
public sealed class QueryWorkbenchRunModeJsonConverter : JsonConverter<QueryWorkbenchRunMode>
{
    public override QueryWorkbenchRunMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType) {
            case JsonTokenType.String: {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s))
                    return QueryWorkbenchRunMode.Query;

                if (s.Equals("Query", StringComparison.OrdinalIgnoreCase) || s.Equals("QueryConcrete", StringComparison.OrdinalIgnoreCase))
                    return QueryWorkbenchRunMode.Query;

                if (s.Equals("QuerySelect", StringComparison.OrdinalIgnoreCase) || s.Equals("QueryProject", StringComparison.OrdinalIgnoreCase))
                    return QueryWorkbenchRunMode.QueryProject;

                if (s.Equals("RootQuery", StringComparison.OrdinalIgnoreCase) || s.Equals("QueryRoot", StringComparison.OrdinalIgnoreCase))
                    return QueryWorkbenchRunMode.RootQuery;

                break;
            }
            case JsonTokenType.Number: {
                var n = reader.GetInt32();
                return n switch {
                    1 => QueryWorkbenchRunMode.QueryProject,
                    2 => QueryWorkbenchRunMode.RootQuery,
                    var _ => QueryWorkbenchRunMode.Query
                };
            }
        }

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