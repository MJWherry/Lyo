using System.Text.Json.Serialization;
using Lyo.Web.Components.JsonEditor;

namespace Lyo.Query.Web.Components;

public sealed record QueryWorkbenchRunConfiguration
{
    public Dictionary<string, List<string>> HostEndpoints { get; init; } = new();

    public string? SelectedHost { get; init; }

    public string Route { get; init; } = "";

    [JsonConverter(typeof(QueryWorkbenchRunModeJsonConverter))]
    public QueryWorkbenchRunMode RunMode { get; init; } = QueryWorkbenchRunMode.Query;

    public double LeftPanePercent { get; init; } = 50;

    public JsonEditorViewMode RequestEditorViewMode { get; init; } = JsonEditorViewMode.Tree;

    public static Dictionary<string, List<string>> CloneHostEndpoints(Dictionary<string, List<string>> source)
    {
        return source.ToDictionary(static kvp => kvp.Key, kvp => kvp.Value.ToList());
    }
}
