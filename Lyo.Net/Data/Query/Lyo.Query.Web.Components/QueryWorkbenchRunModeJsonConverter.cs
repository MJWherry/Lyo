using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Query.Web.Components;

/// <summary>Accepts legacy <c>QuerySelect</c> string/ordinal from persisted workbench JSON after the route was renamed to <see cref="QueryWorkbenchRunMode.QueryProject"/>.</summary>
public sealed class QueryWorkbenchRunModeJsonConverter : JsonConverter<QueryWorkbenchRunMode>
{
    public override QueryWorkbenchRunMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType) {
            case JsonTokenType.String: {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s))
                    return QueryWorkbenchRunMode.Query;

                if (s.Equals("Query", StringComparison.OrdinalIgnoreCase))
                    return QueryWorkbenchRunMode.Query;

                // Legacy persisted name before /QueryProject
                if (s.Equals("QuerySelect", StringComparison.OrdinalIgnoreCase)
                    || s.Equals("QueryProject", StringComparison.OrdinalIgnoreCase))
                    return QueryWorkbenchRunMode.QueryProject;

                break;
            }
            case JsonTokenType.Number: {
                var n = reader.GetInt32();
                return n == 1 ? QueryWorkbenchRunMode.QueryProject : QueryWorkbenchRunMode.Query;
            }
        }

        throw new JsonException($"Unrecognized {nameof(QueryWorkbenchRunMode)} value.");
    }

    public override void Write(Utf8JsonWriter writer, QueryWorkbenchRunMode value, JsonSerializerOptions options)
    {
        var name = value switch {
            QueryWorkbenchRunMode.Query => nameof(QueryWorkbenchRunMode.Query),
            QueryWorkbenchRunMode.QueryProject => nameof(QueryWorkbenchRunMode.QueryProject),
            _ => nameof(QueryWorkbenchRunMode.Query)
        };
        writer.WriteStringValue(name);
    }
}
