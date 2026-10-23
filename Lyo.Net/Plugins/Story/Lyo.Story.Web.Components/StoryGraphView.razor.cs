using System.Globalization;
using Lyo.Diagram.Models;

namespace Lyo.Story.Web.Components;

/// <summary>Draws a story graph from <see cref="StoryGraphDiagram" />. Click a node to select it.</summary>
public partial class StoryGraphView
{
    private static readonly string[] SceneStrokes = ["var(--mud-palette-info)", "var(--mud-palette-success)", "var(--mud-palette-warning)", "var(--mud-palette-secondary)"];

    private StoryGraphPicture _picture = StoryGraphDiagram.Create("Story", new());

    /// <summary>Graph to draw.</summary>
    [Parameter]
    [EditorRequired]
    public StoryDraft Draft { get; set; } = new();

    /// <summary>Key of the node the editor is showing.</summary>
    [Parameter]
    public string? SelectedNodeKey { get; set; }

    /// <summary>Raised with the node key when a box is activated.</summary>
    [Parameter]
    public EventCallback<string> OnNodeSelected { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => _picture = StoryGraphDiagram.Create(Draft.Title, Draft);

    private static string ViewBox(DiagramPage page) => string.Create(CultureInfo.InvariantCulture, $"0 0 {page.PageWidth} {page.PageHeight}");

    private bool IsSelected(DiagramCell node) => KeyOf(node) == SelectedNodeKey;

    private Task SelectAsync(DiagramCell node)
    {
        var key = KeyOf(node);
        return key is null ? Task.CompletedTask : OnNodeSelected.InvokeAsync(key);
    }

    private Task OnNodeKeyDown(KeyboardEventArgs args, DiagramCell node) => args.Key is "Enter" or " " ? SelectAsync(node) : Task.CompletedTask;

    private static string Polyline(DiagramCell edge)
    {
        var geometry = edge.Geometry;
        if (geometry?.SourcePoint is null || geometry.TargetPoint is null)
            return "";

        var points = new List<DiagramPoint> { geometry.SourcePoint };
        points.AddRange(geometry.Points);
        points.Add(geometry.TargetPoint);
        return string.Join(' ', points.Select(point => string.Create(CultureInfo.InvariantCulture, $"{point.X},{point.Y}")));
    }

    private static string Arrow(DiagramCell edge)
    {
        if (edge.Geometry?.TargetPoint is not { } tip)
            return "";

        var x = tip.X;
        var y = tip.Y;
        return string.Create(CultureInfo.InvariantCulture, $"{x},{y} {x - 8},{y - 4} {x - 8},{y + 4}");
    }

    private static double TextX(DiagramCell node) => node.Geometry!.X!.Value + node.Geometry.Width!.Value / 2;

    private static double TextY(DiagramCell node, double fraction) => node.Geometry!.Y!.Value + node.Geometry.Height!.Value * fraction;

    private string Stroke(DiagramCell node)
    {
        if (IsSelected(node))
            return "var(--mud-palette-primary)";

        var scene = NodeOf(node)?.SceneKey;
        if (string.IsNullOrWhiteSpace(scene))
            return "var(--mud-palette-lines-default)";

        var index = Draft.Scenes.FindIndex(item => item.Key == scene);
        if (index < 0)
            index = Math.Abs(StringComparer.Ordinal.GetHashCode(scene));

        return SceneStrokes[index % SceneStrokes.Length];
    }

    private string Label(DiagramCell node)
    {
        var draft = NodeOf(node);
        return string.IsNullOrWhiteSpace(draft?.Title) ? node.Value ?? "" : draft.Title;
    }

    private string Caption(DiagramCell node)
    {
        var draft = NodeOf(node);
        if (draft is null)
            return "";

        var parts = new List<string>();
        if (draft.Key == Draft.StartNodeKey)
            parts.Add("Start");

        if (draft.IsEnding)
            parts.Add("Ending");

        parts.Add(draft.PresentationKind.ToString());
        return string.Join(" · ", parts);
    }

    private string Tooltip(DiagramCell node)
    {
        var draft = NodeOf(node);
        if (draft is null)
            return node.Value ?? "";

        var choices = draft.Choices.Count == 1 ? "1 choice" : $"{draft.Choices.Count} choices";
        return string.IsNullOrWhiteSpace(draft.SceneKey) ? $"{draft.Key}. {choices}." : $"{draft.Key}. Scene {draft.SceneKey}. {choices}.";
    }

    private StoryNodeDraft? NodeOf(DiagramCell node)
    {
        var key = KeyOf(node);
        return key is null ? null : Draft.Nodes.FirstOrDefault(item => item.Key == key);
    }

    private static string? KeyOf(DiagramCell node) => node.Id is { } id && id.StartsWith("k-", StringComparison.Ordinal) ? id[2..] : node.Value;
}