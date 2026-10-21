using System.Globalization;
using Lyo.Diagram;
using Lyo.Diagram.Models;
using Lyo.Exceptions;

namespace Lyo.Story.Web.Components;

/// <summary>A story drawn as a <see cref="DiagramDocument" />, plus the node keys left off because they form a cycle.</summary>
/// <param name="Document">One page whose shapes are nodes and whose edges are choices.</param>
/// <param name="Cyclic">Node keys that were not drawn.</param>
public sealed record StoryGraphPicture(DiagramDocument Document, IReadOnlyList<string> Cyclic);

/// <summary>Projects a story draft onto a diagram. Positions come from <see cref="StoryGraphLayout" />.</summary>
public static class StoryGraphDiagram
{
    /// <summary>Builds the picture for <paramref name="draft" />. A blank <paramref name="name" /> becomes "Story".</summary>
    public static StoryGraphPicture Create(string? name, StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        var layout = StoryGraphLayout.Build(draft);
        var document = new DiagramBuilder(string.IsNullOrWhiteSpace(name) ? "Story" : name).Page("flow", "Flow", page => Write(page, layout)).Build();
        return new(document, layout.Cyclic);
    }

    /// <summary>Cell id for <paramref name="key" />. The <c>k-</c> prefix keeps draw.io's reserved ids <c>0</c> and <c>1</c> out of the document.</summary>
    public static string CellId(string key) => "k-" + key;

    private static void Write(DiagramPageBuilder page, StoryGraphLayout layout)
    {
        if (layout.Width > 0 && layout.Height > 0)
            page.Size(layout.Width, layout.Height);

        foreach (var node in layout.Nodes)
            page.Shape(CellId(node.Key), node.Key, node.X, node.Y, StoryGraphLayout.NodeWidth, StoryGraphLayout.NodeHeight, DiagramShapeKind.Rounded);

        foreach (var edge in layout.Edges)
            page.Connect(EdgeId(edge), CellId(edge.FromKey), CellId(edge.ToKey), Waypoints(edge), Point(edge.StartX, edge.StartY), Point(edge.EndX, edge.EndY), edge.ChoiceKey);
    }

    private static string EdgeId(StoryGraphEdgePlacement edge) => $"e-{edge.FromKey}-{edge.ChoiceKey}-{edge.ToKey}";

    private static DiagramPoint Point(int x, int y) => new() { X = x, Y = y };

    private static IReadOnlyList<DiagramPoint> Waypoints(StoryGraphEdgePlacement edge)
    {
        var tokens = edge.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 12 || tokens[0] != "M" || tokens[3] != "L" || tokens[6] != "L")
            return [];

        return [
            new() { X = int.Parse(tokens[4], CultureInfo.InvariantCulture), Y = int.Parse(tokens[5], CultureInfo.InvariantCulture) },
            new() { X = int.Parse(tokens[7], CultureInfo.InvariantCulture), Y = int.Parse(tokens[8], CultureInfo.InvariantCulture) }
        ];
    }
}