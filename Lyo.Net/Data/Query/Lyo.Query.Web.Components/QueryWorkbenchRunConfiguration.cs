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

    /// <summary>Credential style for Run Query. Default is none.</summary>
    public QueryWorkbenchAuthMode AuthMode { get; init; } = QueryWorkbenchAuthMode.None;

    /// <summary>Header name when <see cref="AuthMode"/> is <see cref="QueryWorkbenchAuthMode.Header"/> (e.g. <c>Authorization</c>, <c>X-Api-Key</c>).</summary>
    public string? AuthHeaderName { get; init; }

    /// <summary>Bearer token or custom header value. Stored with workbench state in the browser.</summary>
    public string? AuthHeaderValue { get; init; }

    public static Dictionary<string, List<string>> CloneHostEndpoints(Dictionary<string, List<string>> source)
        => source.ToDictionary(static kvp => kvp.Key, kvp => kvp.Value.ToList());
}